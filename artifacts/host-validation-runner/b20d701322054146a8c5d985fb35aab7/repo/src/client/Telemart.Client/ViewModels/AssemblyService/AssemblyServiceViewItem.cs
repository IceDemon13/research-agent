using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServiceViewItem : TelemartEditorViewItemBase
    {
        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int? ParentAssemblyServiceId
        {
            get { return GetProperty(() => ParentAssemblyServiceId); }
            set { SetProperty(() => ParentAssemblyServiceId, value, () => RaisePropertyChanged(nameof(KindOfJob))); }
        }

        public int OrderStateId
        {
            get { return GetProperty(() => OrderStateId); }
            set { SetProperty(() => OrderStateId, value); }
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public string NomenclatureSeries
        {
            get { return GetProperty(() => NomenclatureSeries); }
            set { SetProperty(() => NomenclatureSeries, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public DateTime? OrderDeliveryTime
        {
            get { return GetProperty(() => OrderDeliveryTime); }
            set { SetProperty(() => OrderDeliveryTime, value); }
        }

        public DateTime? OrderDeliveryTimeTo
        {
            get { return GetProperty(() => OrderDeliveryTimeTo); }
            set { SetProperty(() => OrderDeliveryTimeTo, value); }
        }

        public DateTime? StartTestOn
        {
            get { return GetProperty(() => StartTestOn); }
            set { SetProperty(() => StartTestOn, value); }
        }

        public int? StartTestBy
        {
            get { return GetProperty(() => StartTestBy); }
            set { SetProperty(() => StartTestBy, value); }
        }

        public DateTime? StartDisassemblyOn
        {
            get { return GetProperty(() => StartDisassemblyOn); }
            set { SetProperty(() => StartDisassemblyOn, value); }
        }

        public int? StartDisassemblyBy
        {
            get { return GetProperty(() => StartDisassemblyBy); }
            set { SetProperty(() => StartDisassemblyBy, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int? OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value); }
        }

        public int? Places
        {
            get { return GetProperty(() => Places); }
            set { SetProperty(() => Places, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public DateTime AssemblyDate
        {
            get { return GetProperty(() => AssemblyDate); }
            set { SetProperty(() => AssemblyDate, value); }
        }

        public DateTime? ArrivedOn
        {
            get { return GetProperty(() => ArrivedOn); }
            set { SetProperty(() => ArrivedOn, value); }
        }

        public DateTime? AssembledOn
        {
            get { return GetProperty(() => AssembledOn); }
            set { SetProperty(() => AssembledOn, value); }
        }

        public int? AssembledBy
        {
            get { return GetProperty(() => AssembledBy); }
            set { SetProperty(() => AssembledBy, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public List<AssemblyServiceProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public List<AdditionalServiceProductDto> AdditionalServices
        {
            get { return GetProperty(() => AdditionalServices); }
            set { SetProperty(() => AdditionalServices, value); }
        }

        public string KindOfJob => ParentAssemblyServiceId.HasValue ? "Разборка" : "Сборка";

        public static void BuildMetadata(MetadataBuilder<AssemblyServiceViewItem> builder)
        {
            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.OrderId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            AssemblyServiceViewItem item = ReflectionObjectCloner.Clone(this);

            item.Products = Products.Select(x =>
            {
                AssemblyServiceProductViewItem viewItem = ReflectionObjectCloner.Clone(x);
                viewItem.SerialNumbers = x.SerialNumbers.ToList();
                return viewItem;
            }).ToList();

            return item;
        }
    }
}