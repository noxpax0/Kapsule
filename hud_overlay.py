"""Transparent radial HUD overlay widget."""

from __future__ import annotations

import math
from collections.abc import Callable

from PySide6.QtCore import QPointF, QRectF, Qt, QPropertyAnimation, QEasingCurve
from PySide6.QtGui import (
    QAction,
    QBrush,
    QColor,
    QConicalGradient,
    QCursor,
    QFont,
    QPainter,
    QPainterPath,
    QPen,
)
from PySide6.QtWidgets import QApplication, QMenu, QStyle, QSystemTrayIcon, QWidget

import actions
import config


ActionRunner = Callable[[str], None]


class HudOverlay(QWidget):
    """Full-screen transparent overlay containing a circular radial menu."""

    def __init__(self, action_runner: ActionRunner) -> None:
        super().__init__()
        self.action_runner = action_runner
        self.options = config.MENU_OPTIONS
        self.hover_index = -1
        self.radius = config.HUD_RADIUS
        self.inner_radius = config.INNER_RADIUS
        self.center = QPointF(0, 0)
        self._closing = False

        self.setWindowFlags(
            Qt.FramelessWindowHint
            | Qt.Tool
            | Qt.WindowStaysOnTopHint
            | Qt.NoDropShadowWindowHint
        )
        self.setAttribute(Qt.WA_TranslucentBackground, True)
        self.setMouseTracking(True)
        self.setFocusPolicy(Qt.StrongFocus)
        self.setWindowOpacity(0.0)

        self.fade = QPropertyAnimation(self, b"windowOpacity", self)
        self.fade.setDuration(150)
        self.fade.setEasingCurve(QEasingCurve.OutCubic)
        self.fade.finished.connect(self._finish_close)

    def toggle(self) -> None:
        if self.isVisible():
            self.close_hud()
        else:
            self.open_hud()

    def open_hud(self) -> None:
        screen = QApplication.screenAt(QCursor.pos()) or QApplication.primaryScreen()
        geometry = screen.geometry()
        self.setGeometry(geometry)
        self.center = QPointF(self.width() / 2, self.height() / 2)
        self.hover_index = -1
        self._closing = False
        self.show()
        self.raise_()
        self.activateWindow()
        self.setFocus(Qt.ActiveWindowFocusReason)
        self.fade.stop()
        self.fade.setStartValue(self.windowOpacity())
        self.fade.setEndValue(1.0)
        self.fade.start()

    def close_hud(self) -> None:
        if not self.isVisible() or self._closing:
            return
        self._closing = True
        self.fade.stop()
        self.fade.setStartValue(self.windowOpacity())
        self.fade.setEndValue(0.0)
        self.fade.start()

    def _finish_close(self) -> None:
        if self._closing and self.windowOpacity() <= 0.01:
            self.hide()
            self._closing = False

    def paintEvent(self, event) -> None:  # noqa: N802 - Qt override
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing, True)

        self._paint_screen_glass(painter)
        self._paint_outer_glow(painter)
        self._paint_segments(painter)
        self._paint_center(painter)
        self._paint_ticks(painter)

    def mouseMoveEvent(self, event) -> None:  # noqa: N802 - Qt override
        index = self._index_at(event.position())
        if index != self.hover_index:
            self.hover_index = index
            self.update()

    def mousePressEvent(self, event) -> None:  # noqa: N802 - Qt override
        if event.button() != Qt.LeftButton:
            return

        index = self._index_at(event.position())
        if index == -1:
            self.close_hud()
            return

        action_name = self.options[index]["action"]
        self.action_runner(action_name)
        self.close_hud()

    def keyPressEvent(self, event) -> None:  # noqa: N802 - Qt override
        if event.key() == Qt.Key_Escape:
            self.close_hud()
        else:
            super().keyPressEvent(event)

    def leaveEvent(self, event) -> None:  # noqa: N802 - Qt override
        self.hover_index = -1
        self.update()
        super().leaveEvent(event)

    def _paint_screen_glass(self, painter: QPainter) -> None:
        painter.fillRect(self.rect(), QColor(2, 8, 14, 48))

    def _paint_outer_glow(self, painter: QPainter) -> None:
        outer = QRectF(
            self.center.x() - self.radius - 22,
            self.center.y() - self.radius - 22,
            (self.radius + 22) * 2,
            (self.radius + 22) * 2,
        )
        gradient = QConicalGradient(self.center, -90)
        gradient.setColorAt(0.0, QColor(38, 217, 255, 48))
        gradient.setColorAt(0.33, QColor(38, 217, 255, 12))
        gradient.setColorAt(0.62, QColor(255, 159, 47, 52))
        gradient.setColorAt(1.0, QColor(38, 217, 255, 48))
        painter.setPen(QPen(QBrush(gradient), 3))
        painter.setBrush(Qt.NoBrush)
        painter.drawEllipse(outer)

    def _paint_segments(self, painter: QPainter) -> None:
        count = len(self.options)
        if count == 0:
            return

        gap_degrees = 4
        sweep = 360 / count
        start_offset = -90 - sweep / 2

        for index, option in enumerate(self.options):
            start = start_offset + index * sweep + gap_degrees / 2
            span = sweep - gap_degrees
            path = self._slice_path(start, span)

            accent = QColor(option.get("accent", "#26d9ff"))
            is_hovered = index == self.hover_index
            fill = QColor(5, 22, 32, 142 if is_hovered else 92)
            edge = QColor(accent)
            edge.setAlpha(245 if is_hovered else 160)

            painter.setPen(QPen(edge, 2.2 if is_hovered else 1.2))
            painter.setBrush(fill)
            painter.drawPath(path)

            if is_hovered:
                glow = QColor(accent)
                glow.setAlpha(58)
                painter.setPen(QPen(glow, 10))
                painter.setBrush(Qt.NoBrush)
                painter.drawPath(path)

            self._paint_label(painter, option["label"], start + span / 2, accent, is_hovered)

    def _paint_center(self, painter: QPainter) -> None:
        center_rect = QRectF(
            self.center.x() - self.inner_radius,
            self.center.y() - self.inner_radius,
            self.inner_radius * 2,
            self.inner_radius * 2,
        )
        painter.setBrush(QColor(3, 12, 18, 188))
        painter.setPen(QPen(QColor(38, 217, 255, 210), 2))
        painter.drawEllipse(center_rect)

        inner = center_rect.adjusted(17, 17, -17, -17)
        painter.setPen(QPen(QColor(255, 159, 47, 190), 1.5))
        painter.drawEllipse(inner)

        font = QFont("Segoe UI", 10, QFont.DemiBold)
        painter.setFont(font)
        painter.setPen(QColor(190, 242, 255, 230))
        painter.drawText(center_rect, Qt.AlignCenter, "CTRL\nHUD")

    def _paint_ticks(self, painter: QPainter) -> None:
        painter.setPen(QPen(QColor(105, 232, 255, 118), 1))
        for tick in range(48):
            angle = math.radians(tick * 7.5 - 90)
            inner = self.radius + (7 if tick % 4 else 0)
            outer = self.radius + (20 if tick % 4 == 0 else 14)
            p1 = QPointF(self.center.x() + math.cos(angle) * inner, self.center.y() + math.sin(angle) * inner)
            p2 = QPointF(self.center.x() + math.cos(angle) * outer, self.center.y() + math.sin(angle) * outer)
            painter.drawLine(p1, p2)

    def _paint_label(
        self,
        painter: QPainter,
        label: str,
        degrees: float,
        accent: QColor,
        is_hovered: bool,
    ) -> None:
        angle = math.radians(degrees)
        label_radius = self.inner_radius + (self.radius - self.inner_radius) * 0.58
        point = QPointF(
            self.center.x() + math.cos(angle) * label_radius,
            self.center.y() + math.sin(angle) * label_radius,
        )
        rect = QRectF(point.x() - 62, point.y() - 16, 124, 32)
        font = QFont("Segoe UI", 10 if len(label) < 12 else 9, QFont.DemiBold)
        painter.setFont(font)
        color = QColor(accent if is_hovered else QColor(207, 246, 255))
        color.setAlpha(255 if is_hovered else 218)
        painter.setPen(color)
        painter.drawText(rect, Qt.AlignCenter, label)

    def _slice_path(self, start_degrees: float, span_degrees: float) -> QPainterPath:
        outer_rect = QRectF(
            self.center.x() - self.radius,
            self.center.y() - self.radius,
            self.radius * 2,
            self.radius * 2,
        )
        inner_rect = QRectF(
            self.center.x() - self.inner_radius,
            self.center.y() - self.inner_radius,
            self.inner_radius * 2,
            self.inner_radius * 2,
        )

        path = QPainterPath()
        path.arcMoveTo(outer_rect, -start_degrees)
        path.arcTo(outer_rect, -start_degrees, -span_degrees)
        path.arcTo(inner_rect, -(start_degrees + span_degrees), span_degrees)
        path.closeSubpath()
        return path

    def _index_at(self, point: QPointF) -> int:
        dx = point.x() - self.center.x()
        dy = point.y() - self.center.y()
        distance = math.hypot(dx, dy)
        if distance < self.inner_radius or distance > self.radius:
            return -1

        count = len(self.options)
        if count == 0:
            return -1

        angle = (math.degrees(math.atan2(dy, dx)) + 360) % 360
        sweep = 360 / count
        adjusted = (angle + 90 + sweep / 2) % 360
        return int(adjusted // sweep) % count


def create_tray_icon(app: QApplication, overlay: HudOverlay) -> QSystemTrayIcon:
    tray = QSystemTrayIcon(app)
    tray.setToolTip("Futuristic Ctrl HUD")
    tray.setIcon(app.style().standardIcon(QStyle.StandardPixmap.SP_ComputerIcon))

    menu = QMenu()
    toggle_action = QAction("Toggle HUD", menu)
    quit_action = QAction("Quit", menu)
    toggle_action.triggered.connect(overlay.toggle)
    quit_action.triggered.connect(app.quit)
    menu.addAction(toggle_action)
    menu.addSeparator()
    menu.addAction(quit_action)
    tray.setContextMenu(menu)
    tray.activated.connect(lambda reason: overlay.toggle() if reason == QSystemTrayIcon.Trigger else None)
    tray.show()
    return tray


def resolve_action(action_name: str):
    action = getattr(actions, action_name, None)
    if not callable(action):
        raise ValueError(f"Unknown action '{action_name}'. Add it to actions.py or update config.py.")
    return action
