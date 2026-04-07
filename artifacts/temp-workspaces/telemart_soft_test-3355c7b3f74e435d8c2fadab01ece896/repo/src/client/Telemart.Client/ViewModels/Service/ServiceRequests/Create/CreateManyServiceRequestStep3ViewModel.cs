using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
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
    public sealed class CreateManyServiceRequestStep3ViewModel
        : WizardPageViewModelBase<CreateManyServiceRequestModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        public CreateManyServiceRequestStep3ViewModel(IWebClient webClient)
        {
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            WebClient = webClient;
        }

        public CreateManyServiceRequestStep3ViewModel()
        {
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public override string Description => "Заполните информацию о заявителе и товаре";

        public override string Header => "Заполнение информации";

        public bool CanGoForward { get; } = true;

        public bool CanGoBack { get; } = true;

        private string[] ValidatableProperties { get; } =
        {
            nameof(ServiceRequestProductSnViewItem.Defect)
        };

        private IWizardService WizardService => GetService<IWizardService>();

        private IWebClient WebClient { get; }

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ReturnMoneyPaymentType = null;
            Model.ClientRequirement = null;
            Model.SelectedProducts.Clear();
            WizardService.NavigateToView<CreateManyServiceRequestStep2ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            foreach (ServiceRequestProductSnViewItem productViewItem in Model.SelectedProducts)
            {
                if (IDataErrorInfoHelper.HasErrors(productViewItem, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
                {
                    return;
                }
            }

            if (Model.ClientRequirement == ServiceRequestRequirement.TradeIn)
            {
                WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }
            else if (Model.ClientRequirement == ServiceRequestRequirement.Change)
            {
                if (Model.HideRequirementPayment)
                {
                    Model.ReturnMoneyPaymentType = Model.HideRequirementPayment
                        ? null
                        : Model.Dictionaries.GetItemById<Payment>(Model.PaymentId!.Value);

                    WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
                else
                {
                    WizardService.NavigateToView<CreateManyServiceRequestStep4ChangeViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
            }
            else if (Model.ClientRequirement == ServiceRequestRequirement.Repair)
            {
                Model.CalculateRepairEnabled();
                WizardService.NavigateToView<CreateManyServiceRequestStep4RepairViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }
            else if (Model.ClientRequirement == ServiceRequestRequirement.ReturnMoney)
            {
                if (Model.HideRequirementPayment)
                {
                    Model.ReturnMoneyPaymentType = Model.HideRequirementPayment
                        ? null
                        : Model.Dictionaries.GetItemById<Payment>(Model.PaymentId!.Value);

                    WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
                else
                {
                    WizardService.NavigateToView<CreateManyServiceRequestStep4ReturnViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
            }

            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            return Task.WhenAll(RefreshContractorsAsync(), RefreshWarehousesAsync(), RefreshCitiesAsync());
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

        private async Task RefreshContractorsAsync()
        {
            if (Model.Contractors == null)
            {
                PagedResult<ContractorDto> contractorsResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);
                Model.Contractors = contractorsResult.Data
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
    }
}