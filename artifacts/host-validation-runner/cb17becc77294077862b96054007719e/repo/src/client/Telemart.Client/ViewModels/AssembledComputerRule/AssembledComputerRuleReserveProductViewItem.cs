using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleReserveProductViewItem : TelemartCloneableViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string ParentCategoryName
        {
            get { return GetProperty(() => ParentCategoryName); }
            set { SetProperty(() => ParentCategoryName, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int ReserveQuantity
        {
            get { return GetProperty(() => ReserveQuantity); }
            set { SetProperty(() => ReserveQuantity, value, () => RaisePropertyChanged(nameof(FactReserveQuantity))); }
        }

        public string SalesQuantity
        {
            get { return GetProperty(() => SalesQuantity); }
            set { SetProperty(() => SalesQuantity, value); }
        }

        public string MinLeftover
        {
            get { return GetProperty(() => MinLeftover); }
            set { SetProperty(() => MinLeftover, value); }
        }

        public int? WarehouseQuantity
        {
            get { return GetProperty(() => WarehouseQuantity); }
            set { SetProperty(() => WarehouseQuantity, value); }
        }

        public int? WarehouseQuantityFree
        {
            get { return GetProperty(() => WarehouseQuantityFree); }
            set { SetProperty(() => WarehouseQuantityFree, value, () => RaisePropertyChanged(nameof(FactReserveQuantity))); }
        }

        public int AssembledComputerRuleQuantity
        {
            get { return GetProperty(() => AssembledComputerRuleQuantity); }
            set { SetProperty(() => AssembledComputerRuleQuantity, value); }
        }

        public int SalesQuantityInsideAssembledComputerRule
        {
            get { return GetProperty(() => SalesQuantityInsideAssembledComputerRule); }
            set { SetProperty(() => SalesQuantityInsideAssembledComputerRule, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value, WarehouseChanged); }
        }

        public int WarehouseQuantityCalculatedForWarehouseId
        {
            get { return GetProperty(() => WarehouseQuantityCalculatedForWarehouseId); }
            set { SetProperty(() => WarehouseQuantityCalculatedForWarehouseId, value); }
        }

        public int FactReserveQuantity
        {
            get { return GetProperty(() => FactReserveQuantity); }
            set { SetProperty(() => FactReserveQuantity, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AssembledComputerRuleReserveProductViewItem> builder)
        {
            builder.Property(x => x.ReserveQuantity)
                .MatchesInstanceRule((x, y) => x > 0, () => "Резерв должен быть больше 0");

            builder.Property(x => x.WarehouseId)
               .Required(() => Resources.RequiredErrorMessage);
        }

        private void WarehouseChanged()
        {
            if (WarehouseQuantityCalculatedForWarehouseId != WarehouseId && WarehouseId > 0 && WarehouseQuantityCalculatedForWarehouseId > 0)
            {
                WarehouseQuantity = null;
                WarehouseQuantityFree = null;
            }
        }
    }
}