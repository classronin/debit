import re
from PySide6.QtCore import QObject, Qt
from PySide6.QtGui import QKeyEvent

class ArithmeticHandler(QObject):
    def __init__(self, editor_tab):
        super().__init__()
        self.editor = editor_tab
        self.editor.installEventFilter(self)

    def eventFilter(self, obj, event):
        if event.type() == QKeyEvent.KeyPress:
            key = event.key()
            modifiers = event.modifiers()
            if modifiers & (Qt.ControlModifier | Qt.AltModifier | Qt.MetaModifier):
                return False
            if key == Qt.Key_Equal:
                self._process_equal_sign()
                return True
        return super().eventFilter(obj, event)

    def _normalize_expression(self, raw: str) -> str:
        raw = raw.replace('（', '(').replace('）', ')')
        raw = raw.replace('×', '*').replace('x', '*').replace('X', '*').replace('÷', '/')
        allowed = re.compile(r'[\d\s\+\-\*/\(\)\.]')
        cleaned = ''.join(allowed.findall(raw))
        return re.sub(r'\s+', '', cleaned)

    def _remove_leading_zeros(self, expr: str) -> str:
        """移除数字的前导零，避免八进制解析错误，但保留单个零和浮点数"""
        def repl(match):
            num = match.group(0)
            if '.' in num:
                return num
            stripped = num.lstrip('0')
            return stripped if stripped else '0'
        return re.sub(r'\b\d+(\.\d+)?\b', repl, expr)

    def _is_valid_expression(self, expr: str) -> bool:
        if not expr:
            return False
        if not any(op in expr for op in '+-*/'):
            return False
        if expr[-1] in '+-*/':
            return False
        if expr.count('(') != expr.count(')'):
            return False
        if not re.match(r'^[\d\+\-\*/\(\)\.]+$', expr):
            return False
        if re.search(r'[\+\*/]{2,}', expr):
            return False
        if expr[0] in '*/':
            return False
        return True

    def _process_equal_sign(self):
        cursor = self.editor.textCursor()
        pos = cursor.position()
        block = cursor.block()
        line_start = block.position()
        line_text = block.text()
        col = pos - line_start
        line_before_cursor = line_text[:col]

        if not line_before_cursor.strip():
            cursor.insertText("=")
            return

        last_eq_pos = line_before_cursor.rfind('=')
        if last_eq_pos == -1:
            raw_text = line_before_cursor
        else:
            raw_text = line_before_cursor[last_eq_pos + 1:]

        expr = self._normalize_expression(raw_text)
        if not self._is_valid_expression(expr):
            cursor.insertText("=")
            return

        # 去除前导零，避免八进制错误
        expr = self._remove_leading_zeros(expr)

        try:
            result = eval(expr, {"__builtins__": None}, {})
            if isinstance(result, (int, float)):
                if isinstance(result, float) and result.is_integer():
                    result_str = str(int(result))
                else:
                    result_str = f"{result:.2f}".rstrip('0').rstrip('.')
            else:
                result_str = str(result)
        except:
            cursor.insertText("=")
            return

        cursor.insertText(f"={result_str}")