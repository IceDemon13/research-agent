using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep2ViewModel : WizardPageViewModelBase<CreateManyServiceRequestModel>, ISupportWizardNextCommand, ISupportWizardBackCommand
    {
        public CreateManyServiceRequestStep2ViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public CreateManyServiceRequestStep2ViewModel()
        {
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public IWebClient WebClient { get; }

        public ReadOnlyObservableCollection<ServiceRequestProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public override string Description => "Необходимо выбрать количество СЗ";

        public override string Header => "Количество СЗ";

        public bool CanGoForward { get; } = true;

        public bool CanGoBack { get; } = true;

        private IWizardService WizardService => GetService<IWizardService>();

        private IMessageFacadeService MessageFacadeService { get; }

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep1ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (!OnGoForwardInternal())
            {
                return;
            }

            foreach (ServiceRequestProductViewItem product in Products)
            {
                for (int i = 0; i < product.CreateQuantity; i++)
                {
                    Model.SelectedProducts.Add(MapToServiceRequestProductSnViewItem(product));
                }
            }

            WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private static ServiceRequestProductViewItem MapToServiceRequestProductViewItem(OrderDto order, OrderProductDto source, IEnumerable<ServiceRequestDto> serviceRequestsInProgress)
        {
            int available = source.Quantity - serviceRequestsInProgress.Count(x => source.SerialNumbers.Select(z => z.SerialNumber).Contains(x.SerialNumber));

            ServiceRequestProductViewItem target = new ServiceRequestProductViewItem()
            {
                ProductId = source.Product.Id,
                Name = source.Product.GetLocalName(LocalizableNameType.Ukr),
                Quantity = source.Quantity,
                SerialNumbers = source.SerialNumbers.ToList(),
                Available = available > 0 ? available : 0,
                CreateQuantity = 0,
                OrderPromoCode = order.PromoCodes?.FirstOrDefault(x => x.PromoCodeId == source.OrderPromoCodeId)
            };

            return target;
        }

        private static ServiceRequestProductSnViewItem MapToServiceRequestProductSnViewItem(ServiceRequestProductViewItem source)
        {
            ServiceRequestProductSnViewItem target = new ServiceRequestProductSnViewItem()
            {
                ProductId = source.ProductId,
                Name = source.Name,
                OrderPromoCode = source.OrderPromoCode,
                SerialNumber = null,
                SerialNumbers = source.SerialNumbers
                    .Select(x => new ServiceRequestSerialNumberViewItem(x.SerialNumber, x.WarrantyRemoved))
                    .ToReadOnlyObservableCollection()
            };

            return target;
        }

        private bool OnGoForwardInternal()
        {
            if (Products.All(x => x.CreateQuantity == 0))
            {
                MessageFacadeService.ShowNotificationWarning("Не выбрано количество СЗ");
                return false;
            }

            return true;
        }

        private async Task HandleLoadedAsync()
        {
            int[] stateIds =
            {
                ServiceRequestState.New.Id,
                ServiceRequestState.Accepted.Id,
                ServiceRequestState.InProgress.Id,
                ServiceRequestState.InRepair.Id,
                ServiceRequestState.OnConfirmation.Id,
                ServiceRequestState.Ready.Id
            };

            ServiceRequestFilteringItem filteringItem = new ServiceRequestFilteringItem()
            {
                OrderNumbers = Model.OrderId.ToString(),
                States = stateIds,
            };

            PagedResult<ServiceRequestDto> serviceRequestsInProgress = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequests(filteringItem));

            Products = Model.Order.Products
                .Select(x => MapToServiceRequestProductViewItem(Model.Order, x, serviceRequestsInProgress.Data))
                .ToReadOnlyObservableCollection();
        }
    }
}