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

Run (after `pip install -e ".[agent]"` on Windows; requires pywin32):
    python -m agent.server
    # (or PYTHONPATH=src python -m src.agent.server before editable install)

This makes the shell's "Test Transport" button report success and
receive a real round-trip instead of the PR 2 "no listener (EXPECTED)".
"""

from __future__ import annotations

import json
import struct
import time
from typing import Any

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


def _recv_exact(handle: int, n: int) -> bytes:
    """Read exactly n bytes from the pipe handle (win32file)."""
    _ensure_win32pipe()
    chunks: list[bytes] = []
    remaining = n
    while remaining > 0:
        data, _ = _WIN32FILE.ReadFile(handle, remaining)
        if not data:
            break
        chunks.append(data)
        remaining -= len(data)
    return b"".join(chunks)


def _send_frame(handle: int, payload: bytes) -> None:
    """Write length-prefixed frame (LE u32 + bytes)."""
    _ensure_win32pipe()
    prefix = struct.pack("<I", len(payload))
    _WIN32FILE.WriteFile(handle, prefix + payload)


def handle_client(handle: int) -> None:
    """Handle a single connected client: read one frame, process, reply."""
    _ensure_win32pipe()
    try:
        # Read length prefix (4 bytes LE)
        len_bytes = _recv_exact(handle, 4)
        if len(len_bytes) != 4:
            return
        (length,) = struct.unpack("<I", len_bytes)
        if length <= 0 or length > 1_000_000:
            return

        body = _recv_exact(handle, length)
        if len(body) != length:
            return

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
        else:
            _send_frame(
                handle, json.dumps({"type": "Error", "error": f"unknown_type:{mtype}"}).encode("utf-8")
            )
    except _PYWINTYPES.error as e:  # type: ignore[attr-defined]
        # Client disconnected or broken pipe - normal during dev
        if e.args[0] not in (109, 232):  # ERROR_BROKEN_PIPE, ERROR_NO_DATA
            print(f"[server] pipe error: {e}")
    except Exception as ex:
        print(f"[server] handler error: {ex}")


def run_server() -> None:
    _ensure_win32pipe()
    print(f"PR 3 agent IPC server listening on {PIPE_NAME}")
    print("Send a Ping from the PR 2 shell 'Test Transport' button or TransportTest.")
    print("Press Ctrl+C to stop.\n")

    while True:
        pipe = _WIN32PIPE.CreateNamedPipe(
            PIPE_NAME,
            _WIN32PIPE.PIPE_ACCESS_DUPLEX,
            _WIN32PIPE.PIPE_TYPE_MESSAGE
            | _WIN32PIPE.PIPE_READMODE_MESSAGE
            | _WIN32PIPE.PIPE_WAIT,
            1,  # max instances
            PIPE_BUFFER,
            PIPE_BUFFER,
            0,
            None,
        )
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
            _WIN32FILE.CloseHandle(pipe)


if __name__ == "__main__":
    run_server()
