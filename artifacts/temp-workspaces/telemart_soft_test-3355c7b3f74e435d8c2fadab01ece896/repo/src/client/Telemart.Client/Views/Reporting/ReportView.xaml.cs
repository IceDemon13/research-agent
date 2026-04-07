using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using Microsoft.Win32;

namespace Telemart.Client.Views.Reporting
{
    /// <summary>
    /// Interaction logic for ReportView.xaml
    /// </summary>
    public partial class ReportView
    {
        public ReportView()
        {
            InitializeComponent();
        }

        private void ExportAnalyzisDataBarButtonOnItemClick(object sender, ItemClickEventArgs e)
        {
            Export(fileName => PivotGridControl.ExportToXlsx(fileName));
        }

        private void ExportReportDataBarButtonOnItemClick(object sender, ItemClickEventArgs e)
        {
            Export(fileName => TableView.ExportToXlsx(fileName));
        }

        private void Export(Action<string> exportAction)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Export Data Dialog",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (.xlsx)|*.xlsx"
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string filename = saveFileDialog.FileName;

                if (!string.IsNullOrWhiteSpace(filename))
                {
                    exportAction(filename);
                }
            }
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            ////if (e.NewItemsSource == null)
            ////{
            ////    return;
            ////}

            ////GridControl gridControl = (GridControl)e.Source;
            ////TableView tableView = (TableView)gridControl.View;
            ////tableView.BestFitColumns();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            if (e.CellSelectionState == SelectionState.None)
            {
                return;
            }

            object result = e.ConditionalValue;

            if (e.Property == TextBlock.ForegroundProperty)
            {
                SolidColorBrush original = e.OriginalValue as SolidColorBrush;
                SolidColorBrush conditional = e.ConditionalValue as SolidColorBrush;

                if (conditional != null && (original == null || original.Color != conditional.Color))
                {
                    result = conditional;
                }
            }

            e.Result = result;
            e.Handled = true;
        }

        private void TableViewOnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            if (e.RowSelectionState == SelectionState.None)
            {
                return;
            }

            object result = e.ConditionalValue;

            if (e.Property == TextBlock.ForegroundProperty)
            {
                SolidColorBrush original = e.OriginalValue as SolidColorBrush;
                SolidColorBrush conditional = e.ConditionalValue as SolidColorBrush;

                if (conditional != null && (original == null || original.Color != conditional.Color))
                {
                    result = conditional;
                }
            }

            e.Result = result;
            e.Handled = true;
        }

        private void TableViewOnShowFilterPopup(object sender, FilterPopupEventArgs e)
        {
            if (e.Column.FilterPopupMode == FilterPopupMode.CheckedList && e.ComboBoxEdit.ItemsSource is List<object> items)
            {
                e.ComboBoxEdit.ItemsSource = GenerateFilterItems(items).ToList();
            }
        }

        private IEnumerable<object> GenerateFilterItems(IEnumerable<object> items)
        {
            yield return new CustomComboBoxItem { DisplayValue = "(Не задано)", EditValue = null };

            foreach (object item in items)
            {
                yield return item;
            }
        }
    }
}
