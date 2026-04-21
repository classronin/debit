"""
主题管理模块 - 使用 QSS 统一样式，彻底告别字体漂移
"""
from PySide6.QtGui import QColor

THEMES = {
    "浅色": {
        "bg": QColor(255, 255, 255),
        "fg": QColor(0, 0, 0),
        "border": QColor(200, 200, 200),
        "selected": QColor(220, 220, 220)
    },
    "深色": {
        "bg": QColor(13, 17, 23),
        "fg": QColor(230, 237, 243),
        "border": QColor(80, 80, 80),
        "selected": QColor(60, 60, 60)
    }
}

def generate_stylesheet(theme_name: str, font_size: int) -> str:
    """生成全局 QSS，确保所有 EditorTab 样式统一"""
    theme = THEMES[theme_name]
    bg = theme["bg"].name()
    fg = theme["fg"].name()
    border = theme["border"].name()
    selected = theme["selected"].name()
    
    return f"""
    QMainWindow {{
        background-color: {bg};
        color: {fg};
        border: 1px solid {border};
    }}
    QTextEdit {{
        color: {fg};
        background-color: {bg};
        selection-background-color: {QColor(100, 150, 200).name()};
        border: none;
    }}
    QStatusBar {{
        background-color: {bg};
        color: {fg};
    }}
    QMenuBar {{
        background-color: {bg};
        color: {fg};
        border-bottom: 1px solid {border};
    }}
    QMenuBar::item:selected {{
        background-color: {selected};
    }}
    """