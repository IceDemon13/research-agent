using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class DeclarantPageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        public DeclarantPageViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
        {
            WebClient = webClient;
            Dictionaries = dictionaries;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public DeclarantPageViewModel()
        {
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public ReadOnlyObservableCollection<ServiceRequestRequirement> ClientRequirements
        {
            get { return GetProperty(() => ClientRequirements); }
            private set { SetProperty(() => ClientRequirements, value); }
        }

        public bool CanGoBack { get; } = true;

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Необходимо заполнить информацию о заявителе";

        public override string Header { get; } = "Заявитель";

        private IDictionaries Dictionaries { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateServiceRequestModel.Fio),
            nameof(CreateServiceRequestModel.Phone),
            nameof(CreateServiceRequestModel.Phone2),
            nameof(CreateServiceRequestModel.Email),
            nameof(CreateServiceRequestModel.ClientRequirement),
            nameof(CreateServiceRequestModel.StatedDefect)
        };

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<SelectProductPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (!IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                object parentViewModel = ((ISupportParentViewModel)this).ParentViewModel;

                OrderDto order = WebClient.ExecuteApiRequest(new QueryOrder(Model.OrderId));

                if (CheckPhone(order.Phone, order.Phone2))
                {
                    Model.CustomerId = order.CustomerId;
                }

                switch (Model.ClientRequirement.Id)
                {
                    case ServiceRequestRequirement.ReturnMoneyId:
                        if (Model.HideRequirementPayment)
                        {
                            Model.ReturnMoneyPaymentType = Model.HideRequirementPayment
                                ? null
                                : Dictionaries.GetItemById<Payment>(Model.PaymentId!.Value);

                            WizardService.NavigateToView<LogisticsPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                        }
                        else
                        {
                            WizardService.NavigateToView<ReturnMoneyPageViewModel>(Model, parentViewModel);
                        }

                        break;
                    case ServiceRequestRequirement.ChangeId:

                        if (Model.HideRequirementPayment)
                        {
                            string productName = Model.Products.First(x => x.Id == Model.ProductId).DisplayValue;

                            Model.ChangeOnProductId = Model.ProductId;
                            Model.ChangeOnProductName = productName;

                            WizardService.NavigateToView<LogisticsPageViewModel>(Model, parentViewModel);
                        }
                        else
                        {
                            WizardService.NavigateToView<ChangePageViewModel>(Model, parentViewModel);
                        }

                        break;
                    case ServiceRequestRequirement.RepairId:
                        WizardService.NavigateToView<RepairPageViewModel>(Model, parentViewModel);
                        break;
                    case ServiceRequestRequirement.TradeInId:
                        WizardService.NavigateToView<LogisticsPageViewModel>(Model, parentViewModel);
                        break;
                    default:
                        MessageFacadeService.ShowNotificationWarning("Неизвестный тип требования");
                        break;
                }
            }

            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            ClientRequirements = Dictionaries.GetItemById<Subdivision>(Model.SubdivisionId.Value)
                .GetServiceRequestRequirements()
                .ToReadOnlyObservableCollection();

            return Task.WhenAll(RefreshContractorsAsync(), RefreshWarehousesAsync(), RefreshCitiesAsync());
        }

        private async Task RefreshContractorsAsync()
        {
            if (Model.Contractors == null)
            {
                PagedResult<ContractorDto> contractorsResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);
                Model.Contractors = contractorsResult.Data
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshWarehousesAsync()
        {
            if (Model.Warehouses == null)
            {
                PagedResult<WarehouseDto> warehousesResult = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
                Model.Warehouses = warehousesResult.Data
                    .Where(x => x.Active == 1)
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshCitiesAsync()
        {
            if (Model.Cities == null)
            {
                PagedResult<CityDto> citiesResult = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true);
                Model.Cities = citiesResult.Data
                    .Where(x => x.Active)
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        private bool CheckPhone(params string[] phone)
        {
            return (!string.IsNullOrEmpty(Model.Phone) && phone.Contains(Model.Phone)) || (!string.IsNullOrEmpty(Model.Phone2) && phone.Contains(Model.Phone2));
        }
    }
}