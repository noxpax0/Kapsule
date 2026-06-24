"""Futuristic transparent radial HUD menu.

Run with:
    python main.py
"""

from __future__ import annotations

import sys
import traceback

from PySide6.QtCore import QTimer
from PySide6.QtWidgets import QApplication

from actions import ActionContext
from hotkey_listener import CtrlDoubleTapListener
from hud_overlay import HudOverlay, create_tray_icon, resolve_action


class HudApp:
    def __init__(self) -> None:
        self.app = QApplication(sys.argv)
        self.app.setQuitOnLastWindowClosed(False)

        self.overlay = HudOverlay(self.run_action)
        self.tray_icon = create_tray_icon(self.app, self.overlay)
        self.context = ActionContext(app=self.app, tray_icon=self.tray_icon)

        self.listener = CtrlDoubleTapListener()
        self.listener.double_tapped.connect(self.overlay.toggle)
        self.listener.start()

        self.app.aboutToQuit.connect(self.listener.stop)

        if "--show" in sys.argv:
            QTimer.singleShot(350, self.overlay.open_hud)

    def run_action(self, action_name: str) -> None:
        try:
            action = resolve_action(action_name)
            action(self.context)
        except Exception:
            traceback.print_exc()

    def run(self) -> int:
        print("Futuristic Ctrl HUD is running.", flush=True)
        print("Double-tap Ctrl to toggle the overlay, or click the tray icon.", flush=True)
        if "--show" in sys.argv:
            print("Test mode: opening the HUD automatically.", flush=True)
        return self.app.exec()


def main() -> int:
    hud = HudApp()
    return hud.run()


if __name__ == "__main__":
    raise SystemExit(main())
