using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Common.Converters
{
    public sealed class InvoiceProductsToProductsCountConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as ICollection<InvoiceProductViewItem>)?.Sum(x => x.Quantity);
        }
    }
}
