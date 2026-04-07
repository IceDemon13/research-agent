using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Telemart.Client.Common.Converters.Base
{
    public abstract class SelectedItemsConverter<T> : MarkupExtension, IValueConverter
    {
        protected SelectedItemsConverter()
        {
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertBackInternal(value, targetType, parameter, culture);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        private ObservableCollection<T> ConvertBackInternal(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<T> selectedItems = new ObservableCollection<T>();

            List<object> selectedObjects = (List<object>)value;

            if (selectedObjects != null)
            {
                foreach (object selectedObject in selectedObjects)
                {
                    selectedItems.Add((T)selectedObject);
                }
            }

            return selectedItems;
        }
    }
}