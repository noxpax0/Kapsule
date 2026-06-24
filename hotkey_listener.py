"""Global Ctrl double-tap listener."""

from __future__ import annotations

import time

from pynput import keyboard
from PySide6.QtCore import QObject, Signal

import config


class CtrlDoubleTapListener(QObject):
    """Emits double_tapped when either Ctrl key is pressed twice quickly."""

    double_tapped = Signal()

    def __init__(self) -> None:
        super().__init__()
        self._last_ctrl_tap = 0.0
        self._ctrl_is_down = False
        self._listener: keyboard.Listener | None = None

    def start(self) -> None:
        self._listener = keyboard.Listener(on_press=self._on_press, on_release=self._on_release)
        self._listener.daemon = True
        self._listener.start()

    def stop(self) -> None:
        if self._listener:
            self._listener.stop()
            self._listener = None

    def _on_press(self, key: keyboard.Key | keyboard.KeyCode) -> None:
        if key not in (keyboard.Key.ctrl, keyboard.Key.ctrl_l, keyboard.Key.ctrl_r):
            return

        if self._ctrl_is_down:
            return

        self._ctrl_is_down = True
        now = time.monotonic()
        if now - self._last_ctrl_tap <= config.DOUBLE_TAP_SECONDS:
            self._last_ctrl_tap = 0.0
            self.double_tapped.emit()
        else:
            self._last_ctrl_tap = now

    def _on_release(self, key: keyboard.Key | keyboard.KeyCode) -> None:
        if key in (keyboard.Key.ctrl, keyboard.Key.ctrl_l, keyboard.Key.ctrl_r):
            self._ctrl_is_down = False
