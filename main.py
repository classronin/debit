import sys
import os
from PySide6.QtWidgets import QApplication
from PySide6.QtGui import QFont, QFontDatabase
from PySide6.QtCore import Qt
from app_core import DebitMainWindow

if __name__ == "__main__":
    app = QApplication(sys.argv)

    # 加载内嵌字体
    font_path = os.path.join(os.path.dirname(__file__), "SarasaMonoSC-Regular.ttf")
    if os.path.exists(font_path):
        font_id = QFontDatabase.addApplicationFont(font_path)
        if font_id != -1:
            families = QFontDatabase.applicationFontFamilies(font_id)
            font_family = families[0] if families else "Sarasa Mono SC"
        else:
            font_family = "Sarasa Mono SC"
    else:
        font_family = "Sarasa Mono SC"

    font = QFont(font_family)
    font.setStyleHint(QFont.StyleHint.Monospace)
    font.setHintingPreference(QFont.HintingPreference.PreferFullHinting)
    font.setStyleStrategy(QFont.StyleStrategy.PreferAntialias)
    app.setFont(font)

    window = DebitMainWindow(font_family)
    window.show()
    sys.exit(app.exec())