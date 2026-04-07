using System.Windows;
using System.Windows.Data;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.MvvmEnhancements.Grid;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public static class GridColumnHelper
    {
        public const string FieldNamePrefix = "FieldName_";

        public static readonly DependencyProperty BindingPathProperty = DependencyProperty.RegisterAttached(
            "BindingPath",
            typeof(string),
            typeof(GridColumnHelper),
            new PropertyMetadata(null, BindingPathChangedCallback));

        public static readonly DependencyProperty FieldNameProperty = DependencyProperty.RegisterAttached(
            "FieldName",
            typeof(string),
            typeof(GridColumnHelper),
            new PropertyMetadata(null, FieldNameChangedCallback));

        private const string ValuePropertyName = nameof(NullableIntPropertyValue.Value);

        public static void SetBindingPath(DependencyObject element, string value)
        {
            element.SetValue(BindingPathProperty, value);
        }

        public static void SetFieldName(DependencyObject element, string value)
        {
            element.SetValue(FieldNameProperty, value);
        }

        public static string GetBindingPath(DependencyObject element)
        {
            return (string)element.GetValue(BindingPathProperty);
        }

        public static string GetFieldName(DependencyObject element)
        {
            return (string)element.GetValue(FieldNameProperty);
        }

        private static void BindingPathChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridColumn column = (GridColumn)d;

            string path = e.NewValue as string;

            path = $"{path}.{ValuePropertyName}";

            column.Binding = new Binding(path) { Mode = BindingMode.TwoWay };
        }

        private static void FieldNameChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridColumn column = (GridColumn)d;

            if (e.NewValue is string fName && !fName.StartsWith(FieldNamePrefix))
            {
                column.FieldName = $"{FieldNamePrefix}{fName}";
            }
        }
    }
}