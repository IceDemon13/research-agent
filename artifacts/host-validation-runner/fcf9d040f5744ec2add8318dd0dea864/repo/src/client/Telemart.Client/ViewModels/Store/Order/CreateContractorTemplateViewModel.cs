using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class CreateContractorTemplateViewModel : TelemartDialogViewModelBase
    {
        private int contractorId;

        public CreateContractorTemplateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FillFromOrderCommand = new AsyncCommand<int?>(FillFromOrderAsync, x => x.HasValue);
        }

        public CreateContractorTemplateViewModel()
        {
        }

        public IAsyncCommand FillFromOrderCommand { get; }

        #region INPC

        public IReadOnlyCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public IReadOnlyCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public IReadOnlyCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public IReadOnlyCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, () => RaisePropertyChanged(nameof(Name))); }
        }

        public CityDto City
        {
            get { return GetProperty(() => City); }
            set { SetProperty(() => City, value, () => RaisePropertyChanged(nameof(Name))); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public string Name => GenerateName(CarryId, City?.Name, Notation);

        public string Notation
        {
            get { return GetProperty(() => Notation); }
            set { SetProperty(() => Notation, value, () => RaisePropertyChanged(nameof(Name))); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        #endregion

        public ContractorTemplateDto ContractorTemplateToSave { get; private set; }

        public static void BuildMetadata(MetadataBuilder<CreateContractorTemplateViewModel> builder)
        {
            builder.Property(x => x.OrderId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.City).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Subdivisions = Dictionaries.GetItems<Subdivision>();
            CarryTypes = Dictionaries.GetItems<CarryType>();

            await Task.WhenAll(FetchCitiesAsync(), FetchWarehousesAsync());

            Title = "Создание шаблона";
        }

        protected override Task HandleOkAsync()
        {
            if (City != null)
            {
                ContractorTemplateToSave = new ContractorTemplateDto
                {
                    Name = Name,
                    ClientId = contractorId,
                    CarryId = CarryId,
                    CityId = City.Id,
                    WarehouseId = WarehouseId,
                    LastName = LastName,
                    FirstName = FirstName,
                    MiddleName = MiddleName,
                    Phone = Phone,
                    Phone2 = Phone2,
                    Email = Email,
                    Address = Address,
                    DeliveryData = DeliveryData,
                    IsDefault = false
                };

                IsOk = true;
                Close();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning($"Город в заказе {OrderId} должен быть заполнен");
            }

            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            contractorId = (int)parameter;
        }

        private void ClearValues()
        {
            OrderId = null;
            City = null;
            CarryId = 0;
            LastName = null;
            FirstName = null;
            MiddleName = null;
            Phone = null;
            Phone2 = null;
            Email = null;
            Address = null;
            DeliveryData = null;
            Notation = null;
        }

        private async Task FetchCitiesAsync()
        {
            Cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync().ConfigureAwait(false);
        }

        private async Task FetchWarehousesAsync()
        {
            Warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
        }

        private async Task FillFromOrderAsync(int? orderId)
        {
            if (orderId == null)
            {
                return;
            }

            ClearValues();

            try
            {
                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId.Value));

                if (order.ClientId != contractorId)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} не принадлежит выбранному контрагенту");
                    return;
                }

                OrderId = order.Id;

                CityDto city = order.CityId.HasValue
                    ? Cities.FirstOrDefault(x => x.Id == order.CityId.Value)
                    : null;

                if (city == null)
                {
                    MessageFacadeService.ShowNotificationWarning($"Город в заказе {order.Id} должен быть заполнен");
                    return;
                }

                LastName = order.LastName;
                FirstName = order.FirstName;
                MiddleName = order.MiddleName;
                Phone = order.Phone;
                Phone2 = order.Phone2;
                Email = order.Email;

                City = city;
                CarryId = order.CarryId;
                Address = order.Address;
                DeliveryData = order.DeliveryData;
                WarehouseId = order.WarehouseId;
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
            {
                MessageFacadeService.ShowNotificationError($"Заказ №{orderId} не найден");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get order");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке заказа");
            }
        }

        private string GenerateName(int carryId, string city, string notation)
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(carryId);

            if (carryType == null)
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(notation)
                ? $"{carryType.Name} ({city})"
                : $"{carryType.Name} ({city}, {notation})";
        }
    }
}