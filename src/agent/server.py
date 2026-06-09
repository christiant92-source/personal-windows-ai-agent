"""Python agent IPC server skeleton (PR 3).

Named-pipe + JSON server matching the exact client stub from PR 2
(src/ui/WinUI3App/MainPage.xaml.cs + TransportTest) and the IPC spike
recommendation in docs/ipc-spike.md.

Framing:
- 4-byte little-endian unsigned int length prefix (struct '<I')
- UTF-8 JSON payload

Wire messages (JSON objects):
- Client -> Server: {"type": "Ping", "nonce": "<hex>", "ts": <unix_ms>}
- Server -> Client: {"type": "Pong", "nonce": "<echo>", "server_ts": <unix_ms>}
- Client -> Server: {"type": "Chat", "text": "<user message>", "nonce": "<hex>", "ts": <unix_ms>}
- Server -> Client: {"type": "ChatResponse", "text": "<agent reply>", "nonce": "<echo>", "server_ts": <unix_ms>}

Run (after `pip install -e ".[agent]"` on Windows; requires pywin32):
    python -m agent.server
    # (or PYTHONPATH=src python -m src.agent.server before editable install)

This is the core of the PR 3 agent: the server now handles real chat
requests from the shell, routes them (local vs cloud stub), and
returns responses over the named pipe.
"""

from __future__ import annotations

import json
import struct
import time
import urllib.request
from typing import Any

import pywintypes

from agent.router import classify_route

# pywin32 is required only at runtime (when actually serving).
# We import lazily inside run_server() so that:
#   - "import agent.server" and package discovery succeed without it
#   - "python -m agent.server" fails fast with clear guidance
#   - tests / other modules can import the package on non-Windows or pre-install
_WIN32PIPE = None
_WIN32FILE = None
_PYWINTYPES = None

def _ensure_win32pipe():
    global _WIN32PIPE, _WIN32FILE, _PYWINTYPES
    if _WIN32PIPE is not None:
        return
    try:
        import pywintypes as _pywintypes  # type: ignore
        import win32file as _win32file  # type: ignore
        import win32pipe as _win32pipe  # type: ignore
        _PYWINTYPES = _pywintypes
        _WIN32FILE = _win32file
        _WIN32PIPE = _win32pipe
    except ImportError as e:  # pragma: no cover
        raise RuntimeError(
            "PR 3 server requires pywin32 on Windows for named pipes.\n"
            "  pip install -e '.[agent]'\n"
            "Then re-run: python -m agent.server"
        ) from e


PIPE_NAME = r"\\.\pipe\my-agent-ipc"
PIPE_BUFFER = 65536


def _send_frame(handle: int, payload: bytes) -> None:
    """Write length-prefixed frame (sync / non-overlapped pipe)."""
    _ensure_win32pipe()
    prefix = struct.pack("<I", len(payload))
    data = prefix + payload
    hr, nbytes = _WIN32FILE.WriteFile(handle, data)
    if hr == 0:
        try:
            _WIN32FILE.FlushFileBuffers(handle)
        except Exception:
            pass
    else:
        print(f"[server] DEBUG: write hr={hr} nbytes={nbytes}")


def _call_ollama(prompt: str, model: str = "llama3") -> str:
    """Real local backend via Ollama (http://localhost:11434).
    Uses /api/generate for a simple non-streaming completion.
    Falls back gracefully if Ollama is not running or errors.
    This is the first real local path for PR4.
    """
    try:
        url = "http://localhost:11434/api/generate"
        payload = {
            "model": model,
            "prompt": f"You are a helpful local AI assistant. Respond concisely and helpfully.\nUser: {prompt}\nAssistant:",
            "stream": False,
            "options": {"temperature": 0.7, "num_predict": 256}
        }
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            headers={"Content-Type": "application/json"}
        )
        with urllib.request.urlopen(req, timeout=30) as resp:
            result = json.loads(resp.read().decode("utf-8"))
            return result.get("response", f"[local-ollama] No response for: {prompt}").strip()
    except Exception as e:
        # Graceful fallback if Ollama not available
        return f"[local-ollama-unavailable: {str(e)[:80]}] Thanks — I received: \"{prompt}\""


def _local_chat_response(text: str, route: str) -> str:
    """Local backend entry point for PR4.
    For 'local' route: calls real Ollama.
    For cloud route: stub.
    Route is always returned for visibility in the shell.
    """
    if route == "local":
        return _call_ollama(text)
    else:
        return f"[would-route-to-cloud] Thanks — I received: \"{text}\""


def handle_client(handle: int) -> None:
    """Handle a single connected client: read one frame, process, reply.

    Uses synchronous ReadFile/WriteFile. The pipe is created without
    FILE_FLAG_OVERLAPPED and the .NET client uses blocking Connect + sync
    Write/Read, so this is the simplest reliable match for the exact
    length-prefixed JSON Ping/Pong contract from docs/ipc-spike.md.
    """
    _ensure_win32pipe()
    try:
        # Sync read in message mode: one client Write of the full (len+body) frame
        # is delivered as a single message. 4096 is ample for our small Ping JSON.
        hr, data = _WIN32FILE.ReadFile(handle, 4096)
        if hr != 0 or not data:
            print(f"[server] DEBUG: read hr={hr} data_len={len(data) if data else 0}")
            return
        print(f"[server] DEBUG: received message len={len(data)} hex={data[:min(20,len(data))].hex()}...")
        if len(data) < 4:
            return
        length = struct.unpack("<I", data[:4])[0]
        print(f"[server] DEBUG: length={length}")
        if length <= 0 or length > len(data) - 4:
            return
        body = data[4:4+length]

        try:
            msg: dict[str, Any] = json.loads(body.decode("utf-8"))
        except Exception:
            _send_frame(handle, json.dumps({"error": "bad_json"}).encode("utf-8"))
            return

        mtype = msg.get("type")
        nonce = msg.get("nonce", "")

        if mtype == "Ping":
            resp = {
                "type": "Pong",
                "nonce": nonce,
                "server_ts": int(time.time() * 1000),
            }
            _send_frame(handle, json.dumps(resp).encode("utf-8"))
            print(f"[server] Pong -> nonce={nonce[:8]}...")
            # Wait for the client to read the Pong and close its end of the pipe.
            # The ReadFile(1) will unblock (usually with error 109/232) when the
            # .NET client disposes the NamedPipeClientStream. Those errors are
            # swallowed by the outer handler. This ensures we don't close our
            # end until the client has received the data.
            try:
                _WIN32FILE.ReadFile(handle, 1)
            except Exception:
                pass
            time.sleep(0.05)

        elif mtype == "Chat":
            text = msg.get("text", "")
            route = classify_route(text)

            response_text = _local_chat_response(text, route)

            resp = {
                "type": "ChatResponse",
                "text": response_text,
                "route": route,
                "nonce": nonce,
                "server_ts": int(time.time() * 1000),
            }
            _send_frame(handle, json.dumps(resp).encode("utf-8"))
            backend = "ollama" if route == "local" else "cloud-stub"
            print(f"[server] ChatResponse (route={route}, backend={backend}) -> nonce={nonce[:8]}...")

            # Same wait-for-client-close handshake.
            try:
                _WIN32FILE.ReadFile(handle, 1)
            except Exception:
                pass
            time.sleep(0.05)

        else:
            _send_frame(
                handle, json.dumps({"type": "Error", "error": f"unknown_type:{mtype}"}).encode("utf-8")
            )
    except _PYWINTYPES.error as e:  # type: ignore[attr-defined]
        if e.args[0] not in (109, 232):
            print(f"[server] pipe error: {e}")
    except Exception as ex:
        print(f"[server] handler error: {ex}")


def run_server() -> None:
    _ensure_win32pipe()
    print(f"PR 3 agent IPC server listening on {PIPE_NAME}")
    print("Chat with the agent (real messages routed via PR3 core) or use Test Transport for ping.")
    print("Press Ctrl+C to stop.\n")

    while True:
        try:
            pipe = _WIN32PIPE.CreateNamedPipe(
                PIPE_NAME,
                _WIN32PIPE.PIPE_ACCESS_DUPLEX,  # synchronous (no FILE_FLAG_OVERLAPPED)
                _WIN32PIPE.PIPE_TYPE_MESSAGE
                | _WIN32PIPE.PIPE_READMODE_MESSAGE
                | _WIN32PIPE.PIPE_WAIT,
                1,  # max instances
                PIPE_BUFFER,
                PIPE_BUFFER,
                0,
                None,
            )
        except _PYWINTYPES.error as e:
            if getattr(e, 'args', [None])[0] == 231:  # ERROR_PIPE_BUSY
                print("[server] Pipe busy, waiting 1s and retrying...")
                time.sleep(1)
                continue
            raise
        try:
            _WIN32PIPE.ConnectNamedPipe(pipe, None)
            print("[server] client connected")
            handle_client(pipe)
        except KeyboardInterrupt:
            print("\n[server] shutting down")
            break
        except Exception as ex:
            print(f"[server] connect error: {ex}")
        finally:
            try:
                _WIN32PIPE.DisconnectNamedPipe(pipe)
            except Exception:
                pass
            try:
                _WIN32FILE.CloseHandle(pipe)
            except Exception:
                pass


if __name__ == "__main__":
    run_server()
