using System;
using System.Windows;
using System.Windows.Controls;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class GridColumnTemplateSelector : DataTemplateSelector
    {
        public const string WarehousePrefix = "Warehouse_";

        public DataTemplate NameColumnTemplate { get; set; }

        public DataTemplate IdColumnTemplate { get; set; }

        public DataTemplate SpinIntColumnTemplate { get; set; }

        public DataTemplate ImageColumnTemplate { get; set; }

        public DataTemplate ComboColumnTemplate { get; set; }

        public DataTemplate TokenComboColumnTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate template;

            switch (item)
            {
                case GridComboColumnItem x:
                    template = x.IsMultiValue
                        ? TokenComboColumnTemplate
                        : ComboColumnTemplate;
                    break;
                case GridColumnItem x:
                    template = GetDataTemplateByFieldName(x.FieldName);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return template;
        }

        private DataTemplate GetDataTemplateByFieldName(string fieldName)
        {
            DataTemplate template = fieldName.Replace(GridColumnHelper.FieldNamePrefix, string.Empty) switch
            {
                nameof(ProductContentDto.Id) => IdColumnTemplate,
                nameof(ProductContentDto.Name) => NameColumnTemplate,
                nameof(AutoShowcaseDto.CategoryNameUkr) => NameColumnTemplate,
                nameof(AutoShowcaseDto.SegmentId) => NameColumnTemplate,
                nameof(AutoShowcaseDto.MinLeftover) => IdColumnTemplate,
                nameof(AutoShowcaseDto.QuantityFree) => IdColumnTemplate,
                nameof(AutoShowcaseDto.WarehouseCategoryPlan) => IdColumnTemplate,
                nameof(AutoShowcaseDto.CanBuy) => IdColumnTemplate,
                nameof(AutoShowcaseDto.SalesQuantity) => IdColumnTemplate,
                nameof(AutoShowcaseDto.TotalSalesQuantity) => IdColumnTemplate,
                nameof(AutoShowcaseDto.SalesQuantityByWarehouse) => IdColumnTemplate,
                nameof(AutoShowcaseDto.CategoryEmployeeId) => NameColumnTemplate,
                nameof(AutoShowcaseDto.ProductNameUkr) => NameColumnTemplate,
                nameof(AutoShowcaseDto.ProductId) => IdColumnTemplate,
                nameof(AutoShowcaseDto.WarehouseId) => IdColumnTemplate,
                { } str when str.StartsWith(WarehousePrefix)
                             && !str.Contains(nameof(AutoShowcaseDto.ModifiedBy))
                             && !str.Contains(nameof(AutoShowcaseDto.CreatedBy))
                             && !str.Contains(nameof(AutoShowcaseDto.Capacity)) => IdColumnTemplate,
                { } str when str.StartsWith(WarehousePrefix) && str.Contains(nameof(AutoShowcaseDto.Capacity)) => SpinIntColumnTemplate,
                { } str when str.StartsWith(WarehousePrefix) && str.Contains(nameof(AutoShowcaseDto.ModifiedBy)) => ImageColumnTemplate,
                { } str when str.StartsWith(WarehousePrefix) && str.Contains(nameof(AutoShowcaseDto.CreatedBy)) => ImageColumnTemplate,
                { } str when str.StartsWith(WarehousePrefix) && str.Contains(nameof(AutoShowcaseDto.SumCapacity)) => IdColumnTemplate,
                _ => throw new NotSupportedException()
            };

            return template;
        }
    }
}