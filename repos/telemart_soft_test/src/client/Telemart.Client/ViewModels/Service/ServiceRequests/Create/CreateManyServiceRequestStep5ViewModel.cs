using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep5ViewModel :
        WizardPageViewModelBase<CreateManyServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        private bool loaded;

        public CreateManyServiceRequestStep5ViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<CreateManyServiceRequestStep5ViewModel> logger)
        {
            WebClient = webClient;
            Dictionaries = dictionaries;
            MessageFacadeService = messageFacadeService;
            Mapper = mapper;
            Logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            HandleWarehouseChangedCommand = new DelegateCommand(HandleWarehouseChanged);
            HandleCarryInChangedCommand = new DelegateCommand(HandleCarryInChanged);
            SelectDeliveryAddressCommand = new DelegateCommand(SelectDeliveryAddress, () => Model.CarryOutId != null && Model.CarryOutId != CarryType.PickupId);
        }

        public CreateManyServiceRequestStep5ViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand HandleCarryInChangedCommand { get; }

        public IDelegateCommand HandleWarehouseChangedCommand { get; }

        public IDelegateCommand SelectDeliveryAddressCommand { get; }

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Необходимо заполнить информацию для логиста";

        public override string Header { get; } = "Логистика";

        private IDictionaries Dictionaries { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMapper Mapper { get; }

        private IWebClient WebClient { get; }

        private ILogger<CreateManyServiceRequestStep5ViewModel> Logger { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateManyServiceRequestModel.CarryInId),
            nameof(CreateManyServiceRequestModel.CarryOutId),
            nameof(CreateManyServiceRequestModel.WarehouseIn),
            nameof(CreateManyServiceRequestModel.CityId),
        };

        public void OnGoBack(CancelEventArgs e)
        {
            object parentViewModel = ((ISupportParentViewModel)this).ParentViewModel;

            switch (Model.ClientRequirement.Id)
            {
                case ServiceRequestRequirement.ReturnMoneyId:
                    if (Model.HideRequirementPayment)
                    {
                        WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, parentViewModel);
                    }
                    else
                    {
                        WizardService.NavigateToView<CreateManyServiceRequestStep4ReturnViewModel>(Model, parentViewModel);
                    }

                    break;
                case ServiceRequestRequirement.ChangeId:
                    if (Model.HideRequirementPayment)
                    {
                        WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, parentViewModel);
                    }
                    else
                    {
                        WizardService.NavigateToView<CreateManyServiceRequestStep4ChangeViewModel>(Model, parentViewModel);
                    }

                    break;
                case ServiceRequestRequirement.RepairId:
                    WizardService.NavigateToView<CreateManyServiceRequestStep4RepairViewModel>(Model, parentViewModel);
                    break;
                case ServiceRequestRequirement.TradeInId:
                    WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, parentViewModel);
                    break;
                default:
                    MessageFacadeService.ShowNotificationWarning("Неизвестный тип требования");
                    break;
            }

            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                ServiceRequestCreateManyDto dto = Model.GetSaveDto(GetSendTo());

                foreach (ServiceRequestProductSnViewItem product in Model.SelectedProducts)
                {
                    IsProductRemovedDto productRemovedDto = new IsProductRemovedDto()
                    {
                        OrderId = Model.OrderId,
                        ProductId = product.ProductId,
                        Requirement = Model.ClientRequirement.Id,
                    };

                    Result<object> preCreate = WebClient.ExecuteApiRequest(new IsProductRemovedRequest(productRemovedDto));

                    string warning = preCreate.Warnings?.FirstOrDefault();

                    if (warning != null)
                    {
                        bool continueCreating = MessageFacadeService.Confirm($"{product.Name}. {warning}", "Продолжить создание?");

                        if (!continueCreating)
                        {
                            IsLongOperationInProgress = false;

                            return;
                        }
                    }
                }

                Task<Result<List<ServiceRequestDto>>> task = WebClient.ExecuteApiRequestAsync(new CreateManyServiceRequest(dto));

                task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Exception exception = t.Exception?.Flatten().InnerException;
                            Model.ValidationItems = new ObservableCollection<ValidationResultItem>(GetValidationItemsFromException(exception));
                        }

                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            Model.CreatedServiceRequests = t.Result.Data
                            .Select(x => Mapper.Map<ServiceRequestViewItem>(x))
                            .ToObservableCollection();

                            Model.CreatedServiceRequestsDto = t.Result.Data;
                        }

                        WizardService.NavigateToView<CreateManyServiceRequestStep6ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);

                        IsLongOperationInProgress = false;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create service request");
                MessageFacadeService.ShowNotificationError("Ошибка при создании заявки");
                IsLongOperationInProgress = false;
            }

            e.Cancel = true;
        }

        private string GetSendTo()
        {
            return string.Join(" ", GetStringParts());

            IEnumerable<string> GetStringParts()
            {
                yield return Dictionaries.GetItemById<CarryType>(Model.CarryOutId.Value).Name;

                yield return Model.Cities.First(x => x.Id == Model.CityId).Name;

                if (Model.CarryInId.HasValue && Model.CarryInId != CarryType.PickupId)
                {
                    yield return Model.DeliveryData?.Address;
                }
            }
        }

        private Task HandleLoadedAsync()
        {
            loaded = true; // workaround for correct back->forward scenario

            if (Model.CarryInId == null)
            {
                Model.CarryInId = Model.Warehouses.Select(x => x.CityId).Contains(Model.OrderCityId ?? -1)
                    ? CarryType.PickupId
                    : CarryType.NpWarehouseId;
            }

            return Task.CompletedTask;
        }

        private void HandleCarryInChanged()
        {
            if (!loaded)
            {
                return;
            }

            WarehouseDto warehouse = null;
            ReadOnlyObservableCollection<WarehouseDto> warehousesByCarryType = null;

            if (Model.CarryInId.HasValue)
            {
                warehousesByCarryType = Model.Warehouses.ToReadOnlyObservableCollection();

                if (warehousesByCarryType.Count == 1)
                {
                    warehouse = warehousesByCarryType.First();
                }
            }

            Model.WarehousesByCarryType = warehousesByCarryType;
            Model.WarehouseIn = warehouse;

            HandleWarehouseChangedCommand.Execute(null);
        }

        private void HandleWarehouseChanged()
        {
            if (!loaded)
            {
                return;
            }

            int? cityId = null;

            if (Model.CarryInId.HasValue)
            {
                WarehouseDto warehouseIn = Model.WarehousesByCarryType.FirstOrDefault(x => x.Id == Model.WarehouseIn?.Id);
                cityId = warehouseIn?.CityId ?? Model.OrderCityId;
            }

            Model.CityId = cityId;
        }

        private void SelectDeliveryAddress()
        {
            if (Model.CityId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите город");
                return;
            }

            if (Model.CarryOutId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите способ доставки");
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(Model.CarryOutId.Value);

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                Model.CityId.Value,
                Model.DeliveryData?.Address,
                Model.DeliveryData);

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)App.Current.MainWindow.DataContext;

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryData = viewModel.GetDeliveryServiceData();
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = mainWindowViewModel.WorkspaceViewModel.SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, mainWindowViewModel);

                if (viewModel.IsOk)
                {
                    Model.DeliveryData = viewModel.GetDeliveryServiceData();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}