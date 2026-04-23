using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace debit_wpf
{
    public partial class EditorPanel : UserControl
    {
        public event EventHandler? ActiveTabChanged;
        public event EventHandler<TabEventArgs>? TabClosed;

        public EditorTab? CurrentTab =>
            (tabControl.SelectedItem as TabItem)?.Content as EditorTab;

        public int TabCount => tabControl.Items.Count;

        public new double FontSize { get; set; } = 12;

        private System.Windows.Media.SolidColorBrush _currentBg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        private System.Windows.Media.SolidColorBrush _currentFg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);

        public EditorPanel()
        {
            InitializeComponent();
        }

        // ---------- 标签操作（供主窗口调用） ----------
        public void AddNewTab(string? filePath = null, string? content = null)
        {
            var tab = new EditorTab();
            var tabItem = CreateTabItem(tab);

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                tab.LoadFile(filePath);
            else if (!string.IsNullOrEmpty(content))
            {
                tab.textBox.Text = content;
                tab.IsModified = true;
            }

            tabControl.Items.Add(tabItem);
            tabControl.SelectedItem = tabItem;
            tab.SetFontSize(FontSize);
            tab.Focus();
        }

        public System.Collections.Generic.List<MainWindow.TabSession> GetTabsData()
        {
            var list = new System.Collections.Generic.List<MainWindow.TabSession>();
            foreach (TabItem item in tabControl.Items)
            {
                if (item.Content is EditorTab tab)
                {
                    list.Add(new MainWindow.TabSession
                    {
                        FilePath = tab.FilePath,
                        Content = tab.textBox.Text,
                        Modified = tab.IsModified,
                        CursorPosition = tab.textBox.CaretIndex
                    });
                }
            }
            return list;
        }

        public void OpenFiles(string[] paths)
        {
            foreach (var path in paths)
            {
                var exists = tabControl.Items.OfType<TabItem>()
                    .FirstOrDefault(ti => (ti.Content as EditorTab)?.FilePath == path);
                if (exists != null)
                {
                    tabControl.SelectedItem = exists;
                    continue;
                }
                AddNewTab(path);
            }
        }

        public bool SaveCurrentTab()
        {
            var tab = CurrentTab;
            if (tab == null) return false;
            if (string.IsNullOrEmpty(tab.FilePath))
            {
                var dlg = new SaveFileDialog { Filter = "文本文件|*.txt|所有文件|*.*" };
                if (dlg.ShowDialog() == true)
                {
                    tab.SaveAsFile(dlg.FileName);
                    UpdateHeader(tab);
                    return true;
                }
                return false;
            }
            else
            {
                tab.SaveFile();
                return true;
            }
        }

        public void SaveAsCurrentTab()
        {
            var tab = CurrentTab;
            if (tab == null) return;
            var dlg = new SaveFileDialog { Filter = "文本文件|*.txt|所有文件|*.*" };
            if (dlg.ShowDialog() == true)
            {
                tab.SaveAsFile(dlg.FileName);
                UpdateHeader(tab);
            }
        }

        public void CloseCurrentTab()
        {
            if (tabControl.SelectedItem is TabItem item)
                CloseTab(item);
        }

        public void SetFontSize(double size)
        {
            foreach (TabItem item in tabControl.Items)
                (item.Content as EditorTab)?.SetFontSize(size);
        }

        public void ApplyTheme(System.Windows.Media.SolidColorBrush bg, System.Windows.Media.SolidColorBrush fg)
        {
            _currentBg = bg;
            _currentFg = fg;
            foreach (TabItem item in tabControl.Items)
                (item.Content as EditorTab)?.ApplyTheme(bg, fg);
        }

        public void UpdateStatusBar()
        {
            ActiveTabChanged?.Invoke(this, EventArgs.Empty);
        }

        // 返回当前标签的标题信息
        public string GetActiveTabTitle()
        {
            var tab = CurrentTab;
            return tab?.GetTitle() ?? "无";
        }

        // ---------- 内部方法 ----------
        private TabItem CreateTabItem(EditorTab tab)
        {
            var tabItem = new TabItem();
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var titleLabel = new TextBlock { Text = tab.GetTitle() };
            var closeBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (s, e) => CloseTab(tabItem);
            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(closeBtn);
            tabItem.Header = headerPanel;
            tabItem.Content = tab;

            tab.ContentModified += (s, e) =>
            {
                if (tabItem.Header is StackPanel sp && sp.Children[0] is TextBlock tb)
                    tb.Text = tab.GetTitle();
                UpdateStatusBar();
            };
            tab.CursorPositionChanged += (s, e) => UpdateStatusBar();
            tab.ApplyTheme(_currentBg, _currentFg);
            return tabItem;
        }

        private void UpdateHeader(EditorTab tab)
        {
            var item = tabControl.Items.OfType<TabItem>()
                .FirstOrDefault(ti => ti.Content == tab);
            if (item != null && item.Header is StackPanel sp && sp.Children[0] is TextBlock tb)
                tb.Text = tab.GetTitle();
        }

        private void CloseTab(TabItem item)
        {
            var tab = item.Content as EditorTab;
            if (tab == null) return;
            if (tab.IsModified)
            {
                var res = MessageBox.Show($"是否保存对「{tab.GetTitle().TrimEnd('*', ' ')}」的更改？",
                    "debit", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res == MessageBoxResult.Cancel) return;
                if (res == MessageBoxResult.Yes)
                {
                    if (string.IsNullOrEmpty(tab.FilePath))
                    {
                        var dlg = new SaveFileDialog { Filter = "文本文件|*.txt|所有文件|*.*" };
                        if (dlg.ShowDialog() == true)
                            tab.SaveAsFile(dlg.FileName);
                        else
                            return;
                    }
                    else
                        tab.SaveFile();
                }
            }
            tabControl.Items.Remove(item);
            if (tabControl.Items.Count == 0)
                AddNewTab(); // 保证面板至少有一个标签
            UpdateStatusBar();
            TabClosed?.Invoke(this, new TabEventArgs(tab, item));
        }

        public void AddExistingTab(EditorTab tab)
        {
            var tabItem = CreateTabItem(tab);
            tabControl.Items.Add(tabItem);
            tabControl.SelectedItem = tabItem;
            tab.Focus();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateStatusBar();
    }

    public class TabEventArgs : EventArgs
    {
        public EditorTab Tab { get; }
        public TabItem TabItem { get; }
        public TabEventArgs(EditorTab tab, TabItem item)
        {
            Tab = tab;
            TabItem = item;
        }
    }
}