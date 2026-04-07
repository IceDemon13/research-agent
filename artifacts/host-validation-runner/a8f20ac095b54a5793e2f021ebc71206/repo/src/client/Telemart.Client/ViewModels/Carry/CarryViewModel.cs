using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Directories.Contractor;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Carry
{
    public sealed class CarryViewModel : TelemartDialogViewModelBase
    {
        public CarryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;

            RefreshCarryPricesCommand = new AsyncCommand(RefreshCarryPricesAsync);
            EditCarryPriceCommand = new DelegateCommand<CarryPriceViewItem>(EditCarryPrice, x => x != null);
            DeleteCarryPriceCommand = new AsyncCommand<CarryPriceViewItem>(DeleteCarryPriceAsync, x => x != null);
            AddCarryPriceCommand = new DelegateCommand<CarryPriceViewItem>(AddCarryPrice);
            AddCourierCommand = new DelegateCommand(AddCourier);
            RemoveCourierCommand = new DelegateCommand<EmployeeDto>(RemoveCourier);

            Messenger.Register<CarryPriceMessage>(this, OnCarryPriceMessage);
        }

        public CarryViewModel()
        {
        }

        public IAsyncCommand DeleteCarryPriceCommand { get; }

        public IAsyncCommand RefreshCarryPricesCommand { get; }

        public IDelegateCommand AddCarryPriceCommand { get; }

        public IDelegateCommand EditCarryPriceCommand { get; }

        public IDelegateCommand AddCourierCommand { get; }

        public IDelegateCommand RemoveCourierCommand { get; }

        public CarryViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public bool IsCourierTabVisible
        {
            get { return GetProperty(() => IsCourierTabVisible); }
            set { SetProperty(() => IsCourierTabVisible, value); }
        }

        public ReadOnlyObservableCollection<EntityActiveValue> ActiveValues
        {
            get { return GetProperty(() => ActiveValues); }
            private set { SetProperty(() => ActiveValues, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<EmployeeDto> SelectedEmployeeCouriers
        {
            get { return GetProperty(() => SelectedEmployeeCouriers); }
            private set { SetProperty(() => SelectedEmployeeCouriers, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ObservableCollection<CarryPriceViewItem> CarryPrices
        {
            get { return GetProperty(() => CarryPrices); }
            private set { SetProperty(() => CarryPrices, value); }
        }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            await base.HandleLoadedAsync();

            ActiveValues = Dictionaries.GetItems<EntityActiveValue>().ToReadOnlyObservableCollection();

            CarryParameter parameter = (CarryParameter)Parameter;

            CarryDto carry = await WebClient.ExecuteApiRequestAsync(new QueryCarry(parameter.Id));
            List<CarryTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryCarryTypes());
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Employees = (await WebClient.ExecuteApiRequestAsync(new QueryEmployees(null, null, true)).GetPagedResultDataAsync()).ToReadOnlyObservableCollection();

            Contractors = contractors
                .Where(x => x.Id == carry.ContractorId || (x.ParentId == ContractorConstants.RetailContractors && !x.Active))
                .ToReadOnlyObservableCollection();

            Types = types.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            Model = Mapper.Map<CarryViewItem>(carry);

            IsCourierTabVisible = carry.CarryTypeId == CarryType.DeliveryId;

            RefreshSelectedCouriers();

            await RefreshCarryPricesAsync();

            Title = $"Способ доставки \"{Model.Name}\"({Model.Id})";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<CarryDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateCarry(Mapper.Map<CarrySaveDto>(Model)));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Способ доставки сохранен с ошибками";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Способ доставки успешно сохранен");
                }

                Messenger.Send(new CarryMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении способа доставки");
                ShowValidationResultView("Ошибки при сохранении способа доставки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update carry");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении способа доставки");
                Logger.LogError(exception, "Error while updating carry");
            }
        }

        private static CarryPriceViewItem MapPrice(CarryPriceDto dto, CarryPriceViewItem viewItem)
        {
            viewItem.Id = dto.Id;
            viewItem.Cost = dto.Cost;
            viewItem.WeightFrom = dto.WeightFrom;
            viewItem.WeightTo = dto.WeightTo;
            viewItem.Active = dto.Active;

            return viewItem;
        }

        private async Task DeleteCarryPriceAsync(CarryPriceViewItem item)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить стоимость доставки?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteCarryPrice(Model.Id, item.Id));

                MessageFacadeService.ShowNotificationInfo("График доставок успешно удален");

                CarryPrices.Remove(item);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении стоимости доставки");
                ShowValidationResultView("Ошибки при удалении стоимости доставки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete carry price");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting carry prices");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении стоимости доставки");
            }
        }

        private async Task RefreshCarryPricesAsync()
        {
            List<CarryPriceDto> carryPriceDtos = await WebClient.ExecuteApiRequestAsync(new QueryCarryPrices(Model.Id));
            CarryPrices = carryPriceDtos.Select(x => MapPrice(x, new CarryPriceViewItem())).ToObservableCollection();
        }

        private void RefreshSelectedCouriers()
        {
            SelectedEmployeeCouriers = Employees.Where(x => Model.Drivers?.Contains(x.Id) == true).ToObservableCollection();
        }

        private void EditCarryPrice(CarryPriceViewItem item)
        {
            DialogDocumentManagerService.ShowView<CarryPriceEditViewModel>(new CarryPriceEditParameter(Model.Id, item.Id), this);
        }

        private void AddCarryPrice(CarryPriceViewItem item)
        {
            DialogDocumentManagerService.ShowView<CarryPriceEditViewModel>(new CarryPriceEditParameter(Model.Id, 0), this);
        }

        private void AddCourier()
        {
            List<ComboBoxItem> couriers = Employees
                .Where(x => !Model.Drivers.Contains(x.Id) && x.PositionId == EmployeePosition.CourierDriverId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            if (couriers.Any())
            {
                SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(couriers, "Выбор водителя", "Водитель"), this);

                if (viewModel.IsOk)
                {
                    Model.Drivers.Add(viewModel.SelectedItem.Value.Id);
                    RefreshSelectedCouriers();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нет доступных водителей");
            }
        }

        private void RemoveCourier(EmployeeDto courier)
        {
            Model.Drivers.Remove(courier.Id);
            RefreshSelectedCouriers();
        }

        private void OnCarryPriceMessage(CarryPriceMessage message)
        {
            if (message.Entity.CarryId != Model.Id || CarryPrices == null)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    CarryPrices.Insert(0, MapPrice(message.Entity, new CarryPriceViewItem()));
                    break;
                case MessageType.Changed:
                    CarryPrices.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => MapPrice(message.Entity, viewItem));
                    break;
            }
        }
    }
}