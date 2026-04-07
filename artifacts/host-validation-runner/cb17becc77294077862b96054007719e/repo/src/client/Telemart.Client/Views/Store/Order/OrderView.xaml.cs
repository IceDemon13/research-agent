using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Data;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.DragDrop;
using Telemart.Client.Business.Audit;
using Telemart.Client.Common;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderWindow.xaml.
    /// </summary>
    public partial class OrderView
    {
        public OrderView()
        {
            InitializeComponent();

            DataObject.AddCopyingHandler(PhoneEdit, PhoneCopingToClipboardHelper.Handle);
            DataObject.AddCopyingHandler(Phone2Edit, PhoneCopingToClipboardHelper.Handle);
        }

        private static int CompareAuditEntries(AuditEntry auditEntry1, AuditEntry auditEntry2)
        {
            int result;

            if (auditEntry1 == null && auditEntry2 != null)
            {
                result = -1;
            }
            else if (auditEntry1 != null && auditEntry2 == null)
            {
                result = 1;
            }
            else if (auditEntry1 == null)
            {
                result = 0;
            }
            else
            {
                result = auditEntry1.CreatedOn.CompareTo(auditEntry2.CreatedOn);

                if (result == 0)
                {
                    result = string.Compare(auditEntry1.Caption, auditEntry2.Caption, StringComparison.Ordinal);
                }
            }

            return result;
        }

        private void LockButtonOnClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { GridControl.Focus(); }));
        }

        private void HistoryGridControlOnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            if (string.Equals(e.Column.FieldName, nameof(AuditEntry.Caption), StringComparison.Ordinal))
            {
                AuditEntry firstRow = (AuditEntry)HistoryGridControl.GetRow(e.ListSourceRowIndex1);
                AuditEntry secondRow = (AuditEntry)HistoryGridControl.GetRow(e.ListSourceRowIndex2);

                e.Result = e.SortOrder == ColumnSortOrder.Descending
                    ? CompareAuditEntries(firstRow, secondRow)
                    : CompareAuditEntries(secondRow, firstRow);

                e.Handled = true;
            }
        }

        private void TableView_CustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_CustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void ShowCommentButtonOnClick(object sender, RoutedEventArgs e)
        {
            CommentMemoEdit.ShowPopup();
        }

        private void Phone1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is ButtonEdit buttonEdit)
                {
                    buttonEdit.EditValue = Clipboard.GetText().GetLocalPhoneNumber();

                    e.Handled = true;
                }
            }
        }

        private void Phone2_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is TextEdit textEdit)
                {
                    textEdit.EditValue = Clipboard.GetText().GetLocalPhoneNumber();

                    e.Handled = true;
                }
            }
        }

        private void OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is OrderProductViewModel)
            {
                OrderProductViewModel rowItem = grid.CurrentItem as OrderProductViewModel;

                string nameColumn = grid.CurrentColumn?.FieldName;

                string forClipboard = grid.CurrentCellValue?.ToString();

                if (nameColumn == nameof(OrderProductViewModel.Source))
                {
                    if (rowItem?.Source?.Id == OrderProductSourceType.NoProductId && !string.IsNullOrEmpty(forClipboard))
                    {
                        string[] test = forClipboard.Split(' ', ':');

                        string order = test.FirstOrDefault(x => int.TryParse(x.Trim(), out int _) && x.Length >= 2);

                        if (!string.IsNullOrEmpty(order))
                        {
                            forClipboard = order;
                        }
                        else
                        {
                            forClipboard = string.Empty;
                        }
                    }
                }

                Clipboard.Clear();

                if (!string.IsNullOrEmpty(forClipboard))
                {
                    Clipboard.SetData(DataFormats.UnicodeText, forClipboard);
                }

                e.Handled = true;
            }
        }

        private void UnavailableProduct_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://telemart.ua/compare/{e.NavigationUrl}";
            e.Handled = true;
        }

        private void ExternalPaymentLink_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = e.NavigationUrl;
            e.Handled = true;
        }
    }
}