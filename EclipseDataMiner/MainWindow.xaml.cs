using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using EclipseDataMiner.Models;
using EclipseDataMiner.ViewModels;

namespace EclipseDataMiner
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        /// <summary>
        /// プレビュー行のダブルクリックにより該当輪郭をルール定義に追加
        /// </summary>
        private void DiscoveredRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is DiscoveredStructureItem item)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.AddDiscoveredItemToRules(item);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 行選択変更時に選択行を自動スクロール表示
        /// </summary>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem != null)
            {
                dg.ScrollIntoView(dg.SelectedItem);
            }
        }

        /// <summary>
        /// ウィンドウ全体での Delete キー押下を検知し、連続削除とフォーカス追従を実現
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;

            // テキスト入力・編集中の場合は通常の文字削除を優先
            var focused = Keyboard.FocusedElement;
            if (focused is TextBoxBase || e.OriginalSource is TextBoxBase) return;
            if (focused is PasswordBox || e.OriginalSource is PasswordBox) return;
            if (focused is ComboBox || e.OriginalSource is ComboBox) return;

            if (DataContext is MainViewModel vm)
            {
                // Tab 2: 輪郭事前マッピング (インデックス 1)
                if (MainTabControl != null && MainTabControl.SelectedIndex == 1)
                {
                    if (vm.SelectedRule != null && vm.DeleteRuleCommand.CanExecute(null))
                    {
                        vm.DeleteRuleCommand.Execute(null);
                        e.Handled = true;
                        FocusSelectedRow(MappingRulesGrid);
                    }
                }
                // Tab 3: DQP 設定 (インデックス 2)
                else if (MainTabControl != null && MainTabControl.SelectedIndex == 2)
                {
                    if (vm.SelectedDqp != null && vm.DeleteDqpCommand.CanExecute(null))
                    {
                        vm.DeleteDqpCommand.Execute(null);
                        e.Handled = true;
                        FocusSelectedRow(DqpGrid);
                    }
                }
            }
        }

        /// <summary>
        /// ルール削除ボタン押下後に DataGrid の新選択行へフォーカスを復帰
        /// </summary>
        private void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(MappingRulesGrid);
        }

        /// <summary>
        /// DQP削除ボタン押下後に DataGrid の新選択行へフォーカスを復帰
        /// </summary>
        private void DeleteDqpButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(DqpGrid);
        }

        /// <summary>
        /// 正規表現ヒントポップアップを閉じる
        /// </summary>
        private void CloseRegexHintsPopup_Click(object sender, RoutedEventArgs e)
        {
            if (BtnRegexHints != null)
            {
                BtnRegexHints.IsChecked = false;
            }
        }

        /// <summary>
        /// スニペット挿入時にポップアップを閉じ、DataGrid にフォーカスを戻す
        /// </summary>
        private void InsertSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (BtnRegexHints != null)
            {
                BtnRegexHints.IsChecked = false;
            }
            FocusSelectedRow(MappingRulesGrid);
        }

        /// <summary>
        /// 計画検索正規表現スニペット挿入後にヒントポップアップを閉じる
        /// </summary>
        private void InsertSearchSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (BtnSearchRegexHints != null)
            {
                BtnSearchRegexHints.IsChecked = false;
            }
        }

        /// <summary>
        /// DataGrid の現在選択行にキーボードフォーカスを確実に設定
        /// </summary>
        private void FocusSelectedRow(DataGrid grid)
        {
            if (grid == null) return;

            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (grid.SelectedItem == null)
                {
                    grid.Focus();
                    return;
                }

                grid.UpdateLayout();
                grid.ScrollIntoView(grid.SelectedItem);

                var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem);
                if (row != null)
                {
                    row.Focus();
                    Keyboard.Focus(row);
                }
                else
                {
                    grid.Focus();
                    Keyboard.Focus(grid);
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// ログテキスト更新時に常に最下行へ自動スクロール
        /// </summary>
        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleTextBox?.ScrollToEnd();
        }
    }
}
