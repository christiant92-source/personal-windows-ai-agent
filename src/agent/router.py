"""Model Router stub (PR 3).

For PR 3: always routes to "local" (stub). Real classification + local/cloud
decision lands in PR 4.
"""

from __future__ import annotations

from typing import Literal


def classify_route(text: str) -> Literal["local", "cloud"]:
    """Return routing decision for the given user utterance or event.

    PR 3 stub: everything is local. PR 4 will implement a real small
    classifier (or rules + LLM fallback) and respect budgets/privacy.
    """
    # TODO(PR4): real classifier, token estimate, PII scan, consent, etc.
    if not text or len(text) < 3:
        return "local"
    # Very naive heuristic for the skeleton (illustrative only)
    lowered = text.lower()
    if any(k in lowered for k in ("research", "search web", "summarize page", "deep ")):
        return "cloud"
    return "local"
