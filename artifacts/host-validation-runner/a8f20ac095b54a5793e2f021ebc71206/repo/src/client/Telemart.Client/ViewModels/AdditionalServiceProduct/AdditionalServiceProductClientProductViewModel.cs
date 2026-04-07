using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductClientProductViewModel : TelemartDialogViewModelBase
    {
        public AdditionalServiceProductClientProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int? AdditionalServiceWarehouseId
        {
            get { return GetProperty(() => AdditionalServiceWarehouseId); }
            set { SetProperty(() => AdditionalServiceWarehouseId, value); }
        }

        public string Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public bool IsLocked
        {
            get { return GetProperty(() => IsLocked); }
            set { SetProperty(() => IsLocked, value); }
        }

        public bool KeepGuestProduct
        {
            get { return GetProperty(() => KeepGuestProduct); }
            set { SetProperty(() => KeepGuestProduct, value, () => RaisePropertiesChanged(nameof(Description), nameof(SerialNumber), nameof(Product))); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AdditionalServiceWarehouses
        {
            get { return GetProperty(() => AdditionalServiceWarehouses); }
            set { SetProperty(() => AdditionalServiceWarehouses, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AdditionalServiceProductClientProductViewModel> builder)
        {
            builder.Property(x => x.Product)
                .MatchesInstanceRule((x, y) => !y.IsLocked || !y.KeepGuestProduct || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Значение не может быть длиннее 100 символов");

            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule((x, y) => !y.IsLocked || !y.KeepGuestProduct || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Значение не может быть длиннее 100 символов");

            builder.Property(x => x.Description)
                .MaxLength(250, () => "Значение не может быть длиннее 250 символов");

            builder.Property(x => x.AdditionalServiceWarehouseId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            AdditionalServiceProductClientProductParameter parameter = (AdditionalServiceProductClientProductParameter)Parameter;

            Product = parameter.Product;
            AdditionalServiceWarehouseId = parameter.AdditionalServiceWarehouseId;
            SerialNumber = parameter.SerialNumber;
            Description = parameter.Description;
            IsLocked = parameter.IsLockedByCurrentEmployee;
            KeepGuestProduct = parameter.KeepProduct;

            Title = IsLocked ? "Заполните характеристики для гостевого товара" : "Характеристики для гостевого товара";

            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses());

            AdditionalServiceWarehouses = warehouses.Data
                .Where(x => x.TypeId is WarehouseKind.PickupId or WarehouseKind.MainId or WarehouseKind.AssemblyId or WarehouseKind.ServiceId && x.Active > 0)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}