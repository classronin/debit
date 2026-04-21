import os
from PySide6.QtCore import Qt
from PySide6.QtWidgets import QTextEdit
from arithmetic import ArithmeticHandler

class EditorTab(QTextEdit):
    def keyPressEvent(self, event):
        """将 Ctrl++ 和 Ctrl+- 快捷键传递给主窗口，确保字体缩放可靠"""
        modifiers = event.modifiers()
        key = event.key()
        # 判断是否为 Ctrl++ 或 Ctrl+-
        if modifiers & Qt.ControlModifier:
            if key == Qt.Key_Plus or key == Qt.Key_Equal:
                # 调用主窗口放大
                from PySide6.QtWidgets import QApplication
                win = QApplication.activeWindow()
                if hasattr(win, 'zoom_in'):
                    win.zoom_in()
                    event.accept()
                    return
            elif key == Qt.Key_Minus:
                from PySide6.QtWidgets import QApplication
                win = QApplication.activeWindow()
                if hasattr(win, 'zoom_out'):
                    win.zoom_out()
                    event.accept()
                    return
        super().keyPressEvent(event)
    
    def __init__(self, file_path=None):
        super().__init__()
        self.file_path = file_path
        self.modified = False
        
        # 设置默认等宽字体
        self.setLineWrapMode(QTextEdit.WidgetWidth)
        
        # 算术处理器
        self.arithmetic = ArithmeticHandler(self)
        
        if file_path and os.path.exists(file_path):
            with open(file_path, 'r', encoding='utf-8') as f:
                self.setPlainText(f.read())
            self.modified = False
        else:
            self.modified = False
            
        self.textChanged.connect(self._on_text_changed)
    
    def _on_text_changed(self):
        self.modified = True
    
    def wheelEvent(self, event):
        """支持 Ctrl + 滚轮缩放字体"""
        if event.modifiers() & Qt.ControlModifier:
            # 获取主窗口
            from PySide6.QtWidgets import QApplication
            main_window = QApplication.activeWindow()
            if main_window and hasattr(main_window, 'zoom_in') and hasattr(main_window, 'zoom_out'):
                delta = event.angleDelta().y()
                if delta > 0:
                    main_window.zoom_in()
                else:
                    main_window.zoom_out()
                event.accept()
                return
        super().wheelEvent(event)
    
    def get_title(self):
        title = os.path.basename(self.file_path) if self.file_path else "未命名"
        return title + (" *" if self.modified else "")
    
    def save(self, save_as=False):
        path = self.file_path
        if save_as or not path:
            from PySide6.QtWidgets import QFileDialog
            path, _ = QFileDialog.getSaveFileName(
                self, "保存文件", "",
                "文本文件 (*.txt);;所有文件 (*.*)"
            )
            if not path:
                return False
        
        try:
            with open(path, 'w', encoding='utf-8') as f:
                f.write(self.toPlainText())
            self.file_path = path
            self.modified = False
            return True
        except Exception as e:
            from PySide6.QtWidgets import QMessageBox
            QMessageBox.critical(self, "错误", f"保存失败：{e}")
            return False
    
    def can_close(self):
        if not self.modified:
            return True
        from PySide6.QtWidgets import QMessageBox
        ret = QMessageBox.question(
            self, "debit",
            f"是否保存对「{self.get_title()}」的更改？",
            QMessageBox.Yes | QMessageBox.No | QMessageBox.Cancel
        )
        if ret == QMessageBox.Yes:
            return self.save()
        elif ret == QMessageBox.No:
            return True
        else:
            return False