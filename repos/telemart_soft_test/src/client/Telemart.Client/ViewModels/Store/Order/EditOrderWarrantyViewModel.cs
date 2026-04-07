using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class EditOrderWarrantyViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler errorHandler;
        private TelemartEnumerableCompareHelper<EditOrderWarrantyViewItem> compareHelper;

        public EditOrderWarrantyViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.errorHandler = errorHandler;
        }

        public EditOrderWarrantyViewModel()
        {
        }

        public ReadOnlyObservableCollection<Warranty> Warranties
        {
            get { return GetProperty(() => Warranties); }
            set { SetProperty(() => Warranties, value); }
        }

        public ReadOnlyObservableCollection<EditOrderWarrantyViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            set { SetProperty(() => OrderProducts, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            OrderId = (int)Parameter;

            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(OrderId));

            Warranties = Dictionaries.GetItems<Warranty>().ToReadOnlyObservableCollection();

            OrderProducts = orderDto.Products
                .OrderBy(x => x.Product.Name)
                .Select(x => new EditOrderWarrantyViewItem(x.WarrantyId, x.Id, x.Product.GetLocalName(LocalizableNameType.Ukr), x.Quantity))
                .ToReadOnlyObservableCollection();

            compareHelper = new TelemartEnumerableCompareHelper<EditOrderWarrantyViewItem>(OrderProducts);

            await base.HandleLoadedAsync();

            Title = $"Изменение гарантии по заказу №{OrderId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            UpdateOrderWarrantyDto saveDto = new(OrderProducts.Select(x => new UpdateOrderWarrantyProductDto(x.OrderProductId, x.WarrantyId)).ToList());

            (await errorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new UpdateOrderWarranty(OrderId, saveDto)), "сохранении гарантии", "Гарантия сохранена", this, true))
                .IfNotNull(_ => CloseOk());
        }
    }
}