using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public class ServiceRequestNomenclatureSeriesProductViewItem : TelemartCloneableViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, RaiseProperties); }
        }

        public int DefaultScannedQuantity
        {
            get { return GetProperty(() => DefaultScannedQuantity); }
            set { SetProperty(() => DefaultScannedQuantity, value, RaiseProperties); }
        }

        public int ScannedQuantity
        {
            get { return GetProperty(() => ScannedQuantity); }
            set { SetProperty(() => ScannedQuantity, value, RaiseProperties); }
        }

        public bool DiagnosticStage
        {
            get { return GetProperty(() => DiagnosticStage); }
            set { SetProperty(() => DiagnosticStage, value, RaiseProperties); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value); }
        }

        public bool KeepSerialOverridden
        {
            get { return GetProperty(() => KeepSerialOverridden); }
            set { SetProperty(() => KeepSerialOverridden, value); }
        }

        public bool ProductFromOldAssembledComputer
        {
            get { return GetProperty(() => ProductFromOldAssembledComputer); }
            set { SetProperty(() => ProductFromOldAssembledComputer, value, RaiseProperties); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public List<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }

        public bool AnalogImageVisible => !DiagnosticStage && Quantity > ScannedQuantity && ProductFromOldAssembledComputer;

        public bool NewImageVisible => !DiagnosticStage && Quantity > ScannedQuantity && !ProductFromOldAssembledComputer;

        public bool ServiceImageVisible => DiagnosticStage && ProductFromOldAssembledComputer && DefaultScannedQuantity > ScannedQuantity;

        public bool IsScannedQuantityValid => !DiagnosticStage || ScannedQuantity == Quantity;

        public static void BuildMetadata(MetadataBuilder<ServiceRequestNomenclatureSeriesProductViewItem> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => x > 0 && x >= y.ScannedQuantity, () => "Плановое количество должно быть больше 0 и не меньше чем просканированное количество");
            builder.Property(x => x.Price)
                .MatchesInstanceRule((x, _) => x > 0, () => "Цена должна быть больше 0");
        }

        private void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(AnalogImageVisible), nameof(NewImageVisible), nameof(ServiceImageVisible));
        }
    }
}