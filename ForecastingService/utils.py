from __future__ import annotations

import logging


def configure_logging(level: str) -> None:
    logging.basicConfig(
        level=getattr(logging, level.upper(), logging.INFO),
        format="%(asctime)s [%(levelname)s] %(name)s - %(message)s",
    )


def get_confidence_label(history_days: int) -> str:
    if history_days >= 60:
        return "High"
    if history_days >= 30:
        return "Medium"
    return "Low"
