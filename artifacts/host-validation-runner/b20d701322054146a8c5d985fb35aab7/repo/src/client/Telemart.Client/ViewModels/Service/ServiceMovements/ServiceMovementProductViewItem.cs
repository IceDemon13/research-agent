using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementProductViewItem : BindableBase, ILocalіzableEntity
    {
        public ServiceMovementProductViewItem()
        {
        }

        public ServiceMovementProductViewItem(int serviceRequestId)
        {
            ServiceRequestId = serviceRequestId;
        }

        public ServiceMovementProductViewItem(int serviceRequestId, string productFullName, string productFullNameUkr, string productFullNameEn, string productSn)
        {
            ServiceRequestId = serviceRequestId;
            ProductFullName = productFullName;
            ProductFullNameUkr = productFullNameUkr;
            ProductFullNameEn = productFullNameEn;
            ProductSn = productSn;
            IsProcessed = true;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public bool Received
        {
            get { return GetProperty(() => Received); }
            set { SetProperty(() => Received, value, () => RaisePropertiesChanged(nameof(State))); }
        }

        public string ProductFullName
        {
            get { return GetProperty(() => ProductFullName); }
            set { SetProperty(() => ProductFullName, value, () => RaisePropertyChanged(nameof(DisplayProductFullName))); }
        }

        public string ProductFullNameUkr
        {
            get { return GetProperty(() => ProductFullNameUkr); }
            set { SetProperty(() => ProductFullNameUkr, value, () => RaisePropertyChanged(nameof(DisplayProductFullName))); }
        }

        public string ProductFullNameEn
        {
            get { return GetProperty(() => ProductFullNameEn); }
            set { SetProperty(() => ProductFullNameEn, value, () => RaisePropertyChanged(nameof(DisplayProductFullName))); }
        }

        public string ProductSn
        {
            get { return GetProperty(() => ProductSn); }
            set { SetProperty(() => ProductSn, value); }
        }

        public bool IsProcessing
        {
            get { return GetProperty(() => IsProcessing); }
            set { SetProperty(() => IsProcessing, value); }
        }

        public bool Scanned
        {
            get { return GetProperty(() => Scanned); }
            set { SetProperty(() => Scanned, value); }
        }

        public bool IsProcessed
        {
            get { return GetProperty(() => IsProcessed); }
            set { SetProperty(() => IsProcessed, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError), nameof(State))); }
        }

        public string ErrorMessage
        {
            get { return GetProperty(() => ErrorMessage); }
            set { SetProperty(() => ErrorMessage, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError))); }
        }

        public decimal? Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int? CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public bool IsSuccess => IsProcessed && string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsError => IsProcessed && !string.IsNullOrWhiteSpace(ErrorMessage);

        public MovementState MovementState
        {
            get { return GetProperty(() => MovementState); }
            set { SetProperty(() => MovementState, value, () => RaisePropertiesChanged(nameof(State))); }
        }

        public MovementState State => Received || IsProcessed ? MovementState.Received : MovementState;

        public string Name => ProductFullName;

        public string NameUkr => ProductFullNameUkr;

        public string NameEn => ProductFullNameEn;

        public string DisplayProductFullName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}