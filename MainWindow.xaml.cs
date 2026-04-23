using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace debit_wpf
{
    public partial class MainWindow : Window
    {
        private const string SessionFile = "session.json";
        private double _currentFontSize = 12;
        private string _currentTheme = "Light";
        private EditorPanel _mainPanel = null!;

        public MainWindow()
        {
            InitializeComponent();
            LightThemeItem.IsChecked = true;

            // 1. 尝试恢复会话，失败则创建默认面板
            if (!RestoreSession())
            {
                _mainPanel = new EditorPanel();
                _mainPanel.AddNewTab();
            }

            // 2. 同步面板字体并绑定事件
            _mainPanel.FontSize = _currentFontSize;
            _mainPanel.SetFontSize(_currentFontSize);
            _mainPanel.ActiveTabChanged += (s, e) => UpdateStatusBar();

            // 3. 应用主题并显示
            ApplyTheme();
            LayoutRoot.Children.Add(_mainPanel);
            UpdateStatusBar();
        }

        // ---------- 菜单事件 ----------
        private void NewFile_Click(object sender, RoutedEventArgs e) => _mainPanel.AddNewTab();
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "文本文件|*.txt|所有文件|*.*", Multiselect = true };
            if (dlg.ShowDialog() == true)
                _mainPanel.OpenFiles(dlg.FileNames);
        }
        private void SaveFile_Click(object sender, RoutedEventArgs e) => _mainPanel.SaveCurrentTab();
        private void SaveAsFile_Click(object sender, RoutedEventArgs e) => _mainPanel.SaveAsCurrentTab();
        private void CloseTab_Click(object sender, RoutedEventArgs e) => _mainPanel.CloseCurrentTab();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // ---------- Ctrl+W Ctrl+S ----------
        private void CtrlW_Executed(object sender, ExecutedRoutedEventArgs e) => _mainPanel.CloseCurrentTab();
        private void CtrlS_Executed(object sender, ExecutedRoutedEventArgs e) => SaveFile_Click(sender, e);

        // ---------- 主题 ----------
        private void ApplyTheme()
        {
            SolidColorBrush bg, fg;
            if (_currentTheme == "Light")
            {
                bg = new SolidColorBrush(Colors.White);
                fg = new SolidColorBrush(Colors.Black);
            }
            else
            {
                bg = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                fg = new SolidColorBrush(Colors.White);
            }
            Background = bg;
            _mainPanel.ApplyTheme(bg, fg);
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            _currentTheme = "Light"; LightThemeItem.IsChecked = true; DarkThemeItem.IsChecked = false;
            ApplyTheme();
        }
        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            _currentTheme = "Dark"; LightThemeItem.IsChecked = false; DarkThemeItem.IsChecked = true;
            ApplyTheme();
        }

        // ---------- 缩放 ----------
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentFontSize = Math.Min(48, _currentFontSize + 2);
            _mainPanel.FontSize = _currentFontSize;
            _mainPanel.SetFontSize(_currentFontSize);
            UpdateStatusBar();
        }
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentFontSize = Math.Max(8, _currentFontSize - 2);
            _mainPanel.FontSize = _currentFontSize;
            _mainPanel.SetFontSize(_currentFontSize);
            UpdateStatusBar();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0) ZoomIn_Click(sender, e); else ZoomOut_Click(sender, e);
                e.Handled = true;
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add) { ZoomIn_Click(sender, e); e.Handled = true; }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract) { ZoomOut_Click(sender, e); e.Handled = true; }
            }
        }

        // ---------- 状态栏 ----------
        private void UpdateStatusBar()
        {
            var tab = _mainPanel.CurrentTab;
            if (tab == null) statusText.Text = "就绪";
            else
            {
                var (line, col) = tab.GetCursorPosition();
                int totalLines = tab.textBox.LineCount;
                string title = string.IsNullOrEmpty(tab.FilePath) ? "未命名" : Path.GetFileName(tab.FilePath);
                title += tab.IsModified ? " *" : "";
                statusText.Text = $"行: {line}/{totalLines}  列: {col}    {title}";
            }
            zoomStatusText.Text = $"字体: {_currentFontSize}pt";
        }

        // ---------- 退出与恢复 ----------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => SaveSession();

        private void SaveSession()
        {
            var session = new
            {
                Theme = _currentTheme,
                FontSize = _currentFontSize,
                WindowState = WindowState.ToString(),
                Left = Left,
                Top = Top,
                Width = Width,
                Height = Height,
                Tabs = _mainPanel.GetTabsData(),
                ActiveTabIndex = _mainPanel.tabControl.SelectedIndex
            };
            File.WriteAllText(SessionFile, JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true }));
        }

        private bool RestoreSession()
        {
            if (!File.Exists(SessionFile)) return false;
            try
            {
                var json = File.ReadAllText(SessionFile);
                var session = JsonSerializer.Deserialize<SessionData>(json);
                if (session == null) return false;

                _currentTheme = session.Theme;
                _currentFontSize = session.FontSize;
                LightThemeItem.IsChecked = _currentTheme == "Light";
                DarkThemeItem.IsChecked = _currentTheme == "Dark";

                // 恢复窗口几何
                if (session.Width > 0 && session.Height > 0)
                {
                    Left = session.Left;
                    Top = session.Top;
                    Width = session.Width;
                    Height = session.Height;
                }
                if (Enum.TryParse(session.WindowState, out WindowState state))
                    WindowState = state;

                // 恢复面板
                _mainPanel = new EditorPanel();
                _mainPanel.FontSize = _currentFontSize;
                foreach (var ts in session.Tabs ?? new List<TabSession>())
                {
                    _mainPanel.AddNewTab(ts.FilePath, ts.Content);
                    var tab = _mainPanel.CurrentTab;
                    if (tab != null && ts.CursorPosition < tab.textBox.Text.Length)
                        tab.textBox.CaretIndex = ts.CursorPosition;
                }
                if (session.Tabs?.Count == 0) _mainPanel.AddNewTab();
                else if (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < _mainPanel.tabControl.Items.Count)
                    _mainPanel.tabControl.SelectedIndex = session.ActiveTabIndex;

                return true;
            }
            catch { return false; }
        }

        // 辅助数据结构（仅用于反序列化）
        private class SessionData
        {
            public string Theme { get; set; } = "Light";
            public double FontSize { get; set; } = 12;
            public string WindowState { get; set; } = "Normal";
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public List<TabSession>? Tabs { get; set; }
            public int ActiveTabIndex { get; set; }
        }

        public class TabSession
        {
            public string? FilePath { get; set; }
            public string Content { get; set; } = string.Empty;
            public bool Modified { get; set; }
            public int CursorPosition { get; set; }
        }
    }
}