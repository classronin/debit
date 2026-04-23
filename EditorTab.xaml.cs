using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace debit_wpf
{
    public partial class EditorTab : UserControl
    {
        public string FilePath { get; set; }
        public bool IsModified { get; set; }
        public event EventHandler? ContentModified;
        public event EventHandler? CursorPositionChanged;

        public EditorTab()
        {
            InitializeComponent();
            FilePath = string.Empty;
            IsModified = false;
            // 使用 PreviewTextInput 捕获等号字符，兼容所有键盘布局
            textBox.PreviewTextInput += TextBox_PreviewTextInput;
        }

        public void LoadFile(string path)
        {
            FilePath = path;
            textBox.Text = File.ReadAllText(path);
            IsModified = false;
            UpdateTitle();
        }

        public void SaveFile()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            File.WriteAllText(FilePath, textBox.Text);
            IsModified = false;
            UpdateTitle();
        }

        public void SaveAsFile(string path)
        {
            FilePath = path;
            File.WriteAllText(path, textBox.Text);
            IsModified = false;
            UpdateTitle();
        }

        public string GetTitle()
        {
            string title = string.IsNullOrEmpty(FilePath) ? "未命名" : Path.GetFileName(FilePath);
            return title + (IsModified ? " *" : "");
        }

        private void UpdateTitle() => ContentModified?.Invoke(this, EventArgs.Empty);

        public void SetFontSize(double size) => textBox.FontSize = size;

        public void ApplyTheme(SolidColorBrush background, SolidColorBrush foreground)
        {
            textBox.Background = background;
            textBox.Foreground = foreground;
        }

        public (int line, int column) GetCursorPosition()
        {
            int line = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
            int column = textBox.CaretIndex - textBox.GetCharacterIndexFromLineIndex(line);
            return (line + 1, column + 1);
        }

        // 拦截文本输入，检测等号并触发计算
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "=")
            {
                ProcessEqualSign();
                e.Handled = true;  // 阻止原来的“=”插入
            }
        }

        private void ProcessEqualSign()
        {
            int caret = textBox.CaretIndex;
            string text = textBox.Text;

            // 找到光标所在行范围
            int lineStart = text.LastIndexOf('\n', caret - 1) + 1;
            int lineEnd = text.IndexOf('\n', caret);
            if (lineEnd == -1) lineEnd = text.Length;

            string line = text.Substring(lineStart, lineEnd - lineStart);
            int col = caret - lineStart;
            string beforeCursor = line.Substring(0, col);

            // 提取最后一个等号之后的表达式
            int lastEq = beforeCursor.LastIndexOf('=');
            string exprRaw = lastEq == -1 ? beforeCursor : beforeCursor.Substring(lastEq + 1);
            exprRaw = exprRaw.Trim();

            if (string.IsNullOrEmpty(exprRaw))
            {
                // 无表达式，仅插入等号
                textBox.SelectedText = "=";
                textBox.CaretIndex += 1;
                return;
            }

            string? result = ArithmeticHandler.Evaluate(exprRaw);
            if (result == null)
            {
                textBox.SelectedText = "=";
                textBox.CaretIndex += 1;
                return;
            }

            string insert = "=" + result;
            textBox.SelectedText = insert;
            textBox.CaretIndex += insert.Length;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsModified)
            {
                IsModified = true;
                UpdateTitle();
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
            => CursorPositionChanged?.Invoke(this, EventArgs.Empty);
    }
}