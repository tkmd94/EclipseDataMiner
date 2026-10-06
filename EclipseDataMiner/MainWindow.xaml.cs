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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            Title = vm.WindowTitle;
        }

        /// <summary>
        /// Double-clicking a preview row adds the corresponding structure to mapping rules.
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
        /// Automatically scrolls the selected row into view when selection changes.
        /// </summary>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem != null)
            {
                dg.ScrollIntoView(dg.SelectedItem);
            }
        }

        /// <summary>
        /// Handles window-wide key presses: Delete for row deletion, and Alt+Up/Down (or Ctrl+Up/Down) for row reordering.
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prioritize normal character editing/cursor movement inside textboxes or dropdowns
            var focused = Keyboard.FocusedElement;
            if (focused is TextBoxBase || e.OriginalSource is TextBoxBase) return;
            if (focused is PasswordBox || e.OriginalSource is PasswordBox) return;
            if (focused is ComboBox || e.OriginalSource is ComboBox) return;

            if (DataContext is MainViewModel vm)
            {
                // Tab 2: Structure Mapping (Index 1)
                if (MainTabControl != null && MainTabControl.SelectedIndex == 1)
                {
                    if (e.Key == Key.Delete)
                    {
                        if (vm.SelectedRule != null && vm.DeleteRuleCommand.CanExecute(null))
                        {
                            vm.DeleteRuleCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(MappingRulesGrid);
                        }
                    }
                    else if ((e.Key == Key.Up || e.Key == Key.Down) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                    {
                        if (e.Key == Key.Up && vm.MoveUpRuleCommand.CanExecute(null))
                        {
                            MappingRulesGrid?.CommitEdit(DataGridEditingUnit.Row, true);
                            MappingRulesGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                            vm.MoveUpRuleCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(MappingRulesGrid);
                        }
                        else if (e.Key == Key.Down && vm.MoveDownRuleCommand.CanExecute(null))
                        {
                            MappingRulesGrid?.CommitEdit(DataGridEditingUnit.Row, true);
                            MappingRulesGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                            vm.MoveDownRuleCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(MappingRulesGrid);
                        }
                    }
                }
                // Tab 3: DQP Configuration (Index 2)
                else if (MainTabControl != null && MainTabControl.SelectedIndex == 2)
                {
                    if (e.Key == Key.Delete)
                    {
                        if (vm.SelectedDqp != null && vm.DeleteDqpCommand.CanExecute(null))
                        {
                            vm.DeleteDqpCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(DqpGrid);
                        }
                    }
                    else if ((e.Key == Key.Up || e.Key == Key.Down) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                    {
                        if (e.Key == Key.Up && vm.MoveUpDqpCommand.CanExecute(null))
                        {
                            DqpGrid?.CommitEdit(DataGridEditingUnit.Row, true);
                            DqpGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                            vm.MoveUpDqpCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(DqpGrid);
                        }
                        else if (e.Key == Key.Down && vm.MoveDownDqpCommand.CanExecute(null))
                        {
                            DqpGrid?.CommitEdit(DataGridEditingUnit.Row, true);
                            DqpGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                            vm.MoveDownDqpCommand.Execute(null);
                            e.Handled = true;
                            FocusSelectedRow(DqpGrid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Commits any active edits in the mapping rules grid before moving to prevent transaction exceptions.
        /// </summary>
        private void MoveRuleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MappingRulesGrid != null)
            {
                MappingRulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                MappingRulesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
        }

        /// <summary>
        /// Restores focus to the selected row in MappingRulesGrid after moving up or down.
        /// </summary>
        private void MoveRuleButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(MappingRulesGrid);
        }

        /// <summary>
        /// Commits any active edits in the DQP grid before moving to prevent transaction exceptions.
        /// </summary>
        private void MoveDqpButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DqpGrid != null)
            {
                DqpGrid.CommitEdit(DataGridEditingUnit.Row, true);
                DqpGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
        }

        /// <summary>
        /// Restores focus to the selected row in DqpGrid after moving up or down.
        /// </summary>
        private void MoveDqpButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(DqpGrid);
        }

        /// <summary>
        /// Restores focus to the newly selected row in DataGrid after clicking Delete Rule button.
        /// </summary>
        private void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(MappingRulesGrid);
        }

        /// <summary>
        /// Restores focus to the newly selected row in DataGrid after clicking Delete DQP button.
        /// </summary>
        private void DeleteDqpButton_Click(object sender, RoutedEventArgs e)
        {
            FocusSelectedRow(DqpGrid);
        }

        /// <summary>
        /// Closes the regular expression hints popup.
        /// </summary>
        private void CloseRegexHintsPopup_Click(object sender, RoutedEventArgs e)
        {
            if (BtnRegexHints != null)
            {
                BtnRegexHints.IsChecked = false;
            }
        }

        /// <summary>
        /// Closes popup and returns focus to DataGrid upon inserting a snippet.
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
        /// Closes search regex hints popup upon inserting a search snippet.
        /// </summary>
        private void InsertSearchSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (BtnSearchRegexHints != null)
            {
                BtnSearchRegexHints.IsChecked = false;
            }
        }

        /// <summary>
        /// Sets keyboard focus to the currently selected row in DataGrid.
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
        /// Automatically scrolls to the bottom of the console textbox when log text is updated.
        /// </summary>
        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleTextBox?.ScrollToEnd();
        }

        /// <summary>
        /// Handles mouse wheel scrolling on DataGrid to ensure smooth and complete scrolling to the very bottom and top.
        /// Prevents scroll locking when mouse cursor hovers over nested cells or dropdowns.
        /// Accumulates target offsets to prevent premature wheel stops caused by asynchronous layout passes.
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(fe);
                if (scrollViewer != null)
                {
                    if (scrollViewer.ScrollableHeight <= 0) return;

                    if (fe.Tag == null)
                    {
                        scrollViewer.ScrollChanged += (s, args) =>
                        {
                            if (args.VerticalChange != 0)
                            {
                                fe.Tag = scrollViewer.VerticalOffset;
                            }
                        };
                    }

                    double currentTarget = (fe.Tag is double tagOffset) ? tagOffset : scrollViewer.VerticalOffset;

                    // Clamp to valid range
                    if (currentTarget < 0) currentTarget = 0;
                    if (currentTarget > scrollViewer.ScrollableHeight) currentTarget = scrollViewer.ScrollableHeight;

                    double step = (Math.Abs(e.Delta) / 120.0) * 48.0;
                    if (step < 16.0) step = 48.0;

                    double newOffset;
                    if (e.Delta < 0)
                    {
                        newOffset = Math.Min(scrollViewer.ScrollableHeight, currentTarget + step);
                    }
                    else
                    {
                        newOffset = Math.Max(0.0, currentTarget - step);
                    }

                    fe.Tag = newOffset;
                    scrollViewer.ScrollToVerticalOffset(newOffset);
                    e.Handled = true;
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}
