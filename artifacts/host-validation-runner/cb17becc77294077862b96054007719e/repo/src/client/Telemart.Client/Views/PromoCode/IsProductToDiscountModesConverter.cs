using System;
using System.Globalization;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.ViewModels.PromoCode;

namespace Telemart.Client.Views.PromoCode
{
    public class IsProductToDiscountModesConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = null;

            if (value is EditGridCellData editGridCellData
                && editGridCellData.Column.DataContext is PromoCodeViewModel viewModel
                && editGridCellData.RowData.Row is PromoCodeProductViewItem item)
            {
                result = item.IsProduct
                    ? viewModel.ProductDiscountModes
                    : viewModel.CategoryDiscountModes;
            }

            return result;
        }
    }
}
