"""Minimal tool framework + registry (PR 3 skeleton).

Tools are simple callables with name/desc/schema for future routing.
All tool calls are audited (stubbed here; real audit JSONL + SQLite in later PR).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Protocol


class Tool(Protocol):
    name: str
    description: str

    def __call__(self, **kwargs: Any) -> Any: ...


_REGISTRY: Dict[str, Tool] = {}


def register(tool: Tool) -> Tool:
    """Decorator or direct call to register a tool."""
    if tool.name in _REGISTRY:
        raise ValueError(f"duplicate tool name: {tool.name}")
    _REGISTRY[tool.name] = tool
    return tool


def registry() -> Dict[str, Tool]:
    return dict(_REGISTRY)


@dataclass
class EchoTool:
    name: str = "echo"
    description: str = "Echo the input (safe test tool)."

    def __call__(self, text: str = "") -> Dict[str, Any]:
        return {"echo": text, "ok": True}


@dataclass
class FileInfoTool:
    name: str = "file_info"
    description: str = "Return basic metadata for a local path (read-only, consented paths only in future)."

    def __call__(self, path: str) -> Dict[str, Any]:
        import os

        try:
            st = os.stat(path)
            return {
                "path": path,
                "size": st.st_size,
                "mtime": st.st_mtime,
                "is_dir": os.path.isdir(path),
                "ok": True,
            }
        except Exception as e:
            return {"path": path, "error": str(e), "ok": False}


# Auto-register the skeleton tools
register(EchoTool())
register(FileInfoTool())
