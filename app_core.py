import sys
import json
import os
from PySide6.QtWidgets import (
    QMainWindow, QTabWidget, QStatusBar, QApplication,
    QFileDialog, QMessageBox
)
from PySide6.QtCore import Qt, QSize
from PySide6.QtGui import QAction, QKeySequence, QActionGroup, QFont
from editor_tab import EditorTab
import theme_manager
from session import save_session, load_session

class DebitMainWindow(QMainWindow):
    def __init__(self, font_family="Sarasa Mono SC"):
        super().__init__()
        self.font_family = font_family
        self.setWindowTitle("debit - 文本算式")
        self.resize(800, 600)

        self.current_theme = "浅色"
        self.font_size = 12

        self.tab_widget = QTabWidget()
        self.tab_widget.setTabsClosable(True)
        self.tab_widget.tabCloseRequested.connect(self.close_tab)
        self.tab_widget.currentChanged.connect(self.update_status)
        self.setCentralWidget(self.tab_widget)

        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)

        self._create_menubar()
        self._apply_theme()
        self._restore_session()
        if self.tab_widget.count() == 0:
            self.new_tab()

    def _create_menubar(self):
        menubar = self.menuBar()
        file_menu = menubar.addMenu("文件(&F)")

        new_action = QAction("新建", self)
        new_action.setShortcut(QKeySequence.New)
        new_action.triggered.connect(lambda: self.new_tab())
        file_menu.addAction(new_action)

        open_action = QAction("打开", self)
        open_action.setShortcut(QKeySequence.Open)
        open_action.triggered.connect(self.open_file)
        file_menu.addAction(open_action)

        save_action = QAction("保存", self)
        save_action.setShortcut(QKeySequence.Save)
        save_action.triggered.connect(self.save_current)
        file_menu.addAction(save_action)

        saveas_action = QAction("另存为", self)
        saveas_action.setShortcut(QKeySequence.SaveAs)
        saveas_action.triggered.connect(self.saveas_current)
        file_menu.addAction(saveas_action)

        file_menu.addSeparator()
        close_tab_action = QAction("关闭标签", self)
        close_tab_action.setShortcut(QKeySequence.Close)
        close_tab_action.triggered.connect(lambda: self.close_tab(self.tab_widget.currentIndex()))
        file_menu.addAction(close_tab_action)

        file_menu.addSeparator()
        exit_action = QAction("退出", self)
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)

        settings_menu = menubar.addMenu("设置(&S)")

        zoom_in_action = QAction("放大", self)
        zoom_in_action.setShortcut(QKeySequence.ZoomIn)
        zoom_in_action.triggered.connect(self.zoom_in)
        settings_menu.addAction(zoom_in_action)

        zoom_out_action = QAction("缩小", self)
        zoom_out_action.setShortcut(QKeySequence.ZoomOut)
        zoom_out_action.triggered.connect(self.zoom_out)
        settings_menu.addAction(zoom_out_action)

        theme_menu = settings_menu.addMenu("主题")
        self.theme_action_group = QActionGroup(self)
        self.theme_action_group.setExclusive(True)
        self.theme_actions = {}
        for name in ["浅色", "深色"]:
            act = QAction(name, self, checkable=True)
            self.theme_action_group.addAction(act)
            act.triggered.connect(lambda checked, n=name: self.set_theme(n))
            theme_menu.addAction(act)
            self.theme_actions[name] = act
        self.theme_actions[self.current_theme].setChecked(True)

    def _apply_theme(self):
        qss = theme_manager.generate_stylesheet(self.current_theme, self.font_size)
        self.setStyleSheet(qss)

    def set_theme(self, name):
        self.current_theme = name
        self._apply_theme()

    def zoom_in(self):
        self.font_size = min(48, self.font_size + 2)
        self._apply_font_to_all_tabs()
        self._apply_theme()
        self.update_status()

    def zoom_out(self):
        self.font_size = max(8, self.font_size - 2)
        self._apply_font_to_all_tabs()
        self._apply_theme()
        self.update_status()

    def _apply_font_to_all_tabs(self):
        font = QFont(self.font_family, self.font_size)
        for i in range(self.tab_widget.count()):
            self.tab_widget.widget(i).setFont(font)

    def new_tab(self, file_path=None):
        tab = EditorTab(file_path)
        index = self.tab_widget.addTab(tab, tab.get_title())
        self.tab_widget.setCurrentIndex(index)
        # 关键：为新标签页设置当前字体大小
        font = QFont(self.font_family, self.font_size)
        tab.setFont(font)
        tab.textChanged.connect(lambda: self._update_tab_title(tab))
        self.update_status()

    def _update_tab_title(self, tab):
        idx = self.tab_widget.indexOf(tab)
        if idx >= 0:
            self.tab_widget.setTabText(idx, tab.get_title())

    def open_file(self):
        paths, _ = QFileDialog.getOpenFileNames(
            self, "打开文件", "",
            "文本文件 (*.txt);;所有文件 (*.*)"
        )
        for path in paths:
            found = False
            for i in range(self.tab_widget.count()):
                tab = self.tab_widget.widget(i)
                if tab.file_path == path:
                    self.tab_widget.setCurrentIndex(i)
                    found = True
                    break
            if not found:
                self.new_tab(path)

    def save_current(self):
        tab = self.tab_widget.currentWidget()
        if tab and tab.save():
            self._update_tab_title(tab)
            self.update_status()

    def saveas_current(self):
        tab = self.tab_widget.currentWidget()
        if tab and tab.save(save_as=True):
            self._update_tab_title(tab)
            self.update_status()

    def close_tab(self, index):
        tab = self.tab_widget.widget(index)
        if tab.can_close():
            self.tab_widget.removeTab(index)
        self.update_status()

    def update_status(self):
        tab = self.tab_widget.currentWidget()
        if tab:
            cursor = tab.textCursor()
            line = cursor.blockNumber() + 1
            col = cursor.columnNumber() + 1
            total_lines = tab.document().blockCount()
            self.status_bar.showMessage(
                f"行: {line}/{total_lines}  列: {col}    "
                f"字体: {self.font_size}pt    {tab.get_title()}"
            )

    def closeEvent(self, event):
        self._save_session()
        event.accept()

    def _save_session(self):
        tabs_data = []
        for i in range(self.tab_widget.count()):
            tab = self.tab_widget.widget(i)
            tabs_data.append({
                "file": tab.file_path,
                "content": tab.toPlainText(),
                "modified": tab.modified,
                "cursor": tab.textCursor().position()
            })
        geom = {
            "x": self.x(), "y": self.y(),
            "width": self.width(), "height": self.height()
        }
        save_session(tabs_data, self.current_theme, self.font_size,
                     self.tab_widget.currentIndex(), geom)

    def _restore_session(self):
        session = load_session()
        if session:
            self.current_theme = session.get("theme", "深色")
            self.font_size = session.get("font_size", 12)
            for tab_info in session.get("tabs", []):
                tab = EditorTab(tab_info["file"])
                if tab_info.get("content"):
                    tab.setPlainText(tab_info["content"])
                tab.modified = tab_info.get("modified", False)
                self.tab_widget.addTab(tab, tab.get_title())
                cursor = tab.textCursor()
                pos = tab_info.get("cursor", 0)
                # 边界检查，防止越界
                if pos > len(tab.toPlainText()):
                    pos = len(tab.toPlainText())
                cursor.setPosition(pos)
                tab.setTextCursor(cursor)
                # 应用字体
                font = QFont(self.font_family, self.font_size)
                tab.setFont(font)
            active = session.get("active", 0)
            if active < self.tab_widget.count():
                self.tab_widget.setCurrentIndex(active)
            geom = session.get("geometry")
            if geom:
                self.setGeometry(geom["x"], geom["y"], geom["width"], geom["height"])
            self._apply_theme()
            if self.current_theme in self.theme_actions:
                self.theme_actions[self.current_theme].setChecked(True)