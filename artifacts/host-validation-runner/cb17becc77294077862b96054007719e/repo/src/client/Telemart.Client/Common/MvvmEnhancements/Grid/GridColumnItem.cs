using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class GridColumnItem : BindableBase
    {
        public GridColumnItem(
            string fieldName,
            string header,
            bool fixedWidth,
            bool editable = false,
            string headerToolTip = null,
            bool columnVisible = true,
            int width = 48,
            bool showInColumnChooser = true,
            HorizontalContentAlignment horizontalContentAlignment = Grid.HorizontalContentAlignment.Center)
        {
            FieldName = fieldName;
            Header = header;
            HeaderToolTip = headerToolTip ?? header;
            FixedWidth = fixedWidth;
            Editable = editable;
            ColumnVisible = columnVisible;
            Width = width;
            ShowInColumnChooser = showInColumnChooser;
            HorizontalContentAlignment = horizontalContentAlignment.ToString();
        }

        public bool Editable
        {
            get { return GetProperty(() => Editable); }
            set { SetProperty(() => Editable, value); }
        }

        public bool ColumnVisible
        {
            get { return GetProperty(() => ColumnVisible); }
            set { SetProperty(() => ColumnVisible, value); }
        }

        public string FieldName
        {
            get { return GetProperty(() => FieldName); }
            set { SetProperty(() => FieldName, value); }
        }

        public bool FixedWidth
        {
            get { return GetProperty(() => FixedWidth); }
            set { SetProperty(() => FixedWidth, value); }
        }

        public string Header
        {
            get { return GetProperty(() => Header); }
            set { SetProperty(() => Header, value); }
        }

        public string HeaderToolTip
        {
            get { return GetProperty(() => HeaderToolTip); }
            set { SetProperty(() => HeaderToolTip, value); }
        }

        public string DisplayFormat
        {
            get { return GetProperty(() => DisplayFormat); }
            set { SetProperty(() => DisplayFormat, value); }
        }

        public int Width
        {
            get { return GetProperty(() => Width); }
            set { SetProperty(() => Width, value); }
        }

        public bool ShowInColumnChooser
        {
            get { return GetProperty(() => ShowInColumnChooser); }
            set { SetProperty(() => ShowInColumnChooser, value); }
        }

        public FilterPopupMode FilterPopupMode
        {
            get { return GetProperty(() => FilterPopupMode); }
            set { SetProperty(() => FilterPopupMode, value); }
        }

        public string HorizontalContentAlignment
        {
            get { return GetProperty(() => HorizontalContentAlignment); }
            set { SetProperty(() => HorizontalContentAlignment, value); }
        }
    }
}