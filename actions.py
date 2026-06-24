"""Actions available to the radial HUD menu.

To add a new action, create a function here that accepts one argument named
context, then reference that function by name in config.py.
"""

from __future__ import annotations

import subprocess
from dataclasses import dataclass
from typing import Any

from PySide6.QtWidgets import QApplication, QSystemTrayIcon


@dataclass
class ActionContext:
    """Objects shared with actions when menu items are clicked."""

    app: QApplication
    tray_icon: QSystemTrayIcon | None = None
    extra: dict[str, Any] | None = None


def print_option_1(context: ActionContext) -> None:
    print("Option 1 selected", flush=True)


def print_option_2(context: ActionContext) -> None:
    print("Option 2 selected", flush=True)


def open_notepad(context: ActionContext) -> None:
    subprocess.Popen(["notepad.exe"])


def show_notification(context: ActionContext) -> None:
    message = "HUD action triggered"
    if context.tray_icon and context.tray_icon.isVisible():
        context.tray_icon.showMessage("Futuristic HUD", message, QSystemTrayIcon.Information, 2500)
    else:
        print(message, flush=True)
