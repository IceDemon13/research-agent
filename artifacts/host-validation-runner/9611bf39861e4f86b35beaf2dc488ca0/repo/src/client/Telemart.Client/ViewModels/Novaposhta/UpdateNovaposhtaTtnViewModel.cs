using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.NovaposhtaTtn;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Novaposhta
{
    public sealed class UpdateNovaposhtaTtnViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private OrderDto _order;
        private CarryType _carryType;

        public UpdateNovaposhtaTtnViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;

            SelectNpWarehouseCommand = new DelegateCommand(SelectNpWarehouse, () => CityId.HasValue);
        }

        public IDelegateCommand SelectNpWarehouseCommand { get; }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value, CityIdChanged); }
        }

        public string RecipientFirstName
        {
            get { return GetProperty(() => RecipientFirstName); }
            set { SetProperty(() => RecipientFirstName, value); }
        }

        public string RecipientLastName
        {
            get { return GetProperty(() => RecipientLastName); }
            set { SetProperty(() => RecipientLastName, value); }
        }

        public string RecipientMiddleName
        {
            get { return GetProperty(() => RecipientMiddleName); }
            set { SetProperty(() => RecipientMiddleName, value); }
        }

        public string RecipientPhone
        {
            get { return GetProperty(() => RecipientPhone); }
            set { SetProperty(() => RecipientPhone, value); }
        }

        public decimal PackageWeight
        {
            get { return GetProperty(() => PackageWeight); }
            set { SetProperty(() => PackageWeight, value); }
        }

        public decimal PackagePlaces
        {
            get { return GetProperty(() => PackagePlaces); }
            set { SetProperty(() => PackagePlaces, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string RecipientAddress
        {
            get { return GetProperty(() => RecipientAddress); }
            private set { SetProperty(() => RecipientAddress, value); }
        }

        public bool AllowEditPackageInfo
        {
            get { return GetProperty(() => AllowEditPackageInfo); }
            private set { SetProperty(() => AllowEditPackageInfo, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public static void BuildMetadata(MetadataBuilder<UpdateNovaposhtaTtnViewModel> builder)
        {
            builder.Property(x => x.RecipientLastName)
                .Required(() => Resources.RequiredErrorMessage)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.RecipientFirstName)
                .Required(() => Resources.RequiredErrorMessage)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.RecipientMiddleName)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.Description)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RecipientPhone)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RecipientAddress)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PackagePlaces).NpPackagePlaces();
            builder.Property(x => x.PackageWeight).NpPackageWeight();
        }

        protected override async Task HandleLoadedAsync()
        {
            UpdateNovaposhtaTtnParameter parameter = (UpdateNovaposhtaTtnParameter)Parameter;

            _order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

            List<DeliveryServicePlaceDto> places = await WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(_order.CarryId, _order.CityId!.Value));

            _carryType = Dictionaries.GetItemById<CarryType>(_order.CarryId);

            CityId = _order.CityId;
            DeliveryData = _order.DeliveryData;
            PackagePlaces = _order.PackagePlaces;
            PackageWeight = (decimal)_order.PackageWeight;
            AllowEditPackageInfo = WebClient.IsOperationAllowed(BusinessOperation.UpdateNpTtn);

            string placeName = places.FirstOrDefault(x => x.PlaceId == _order.DeliveryData.PlaceId)?.PlaceName;

            RecipientAddress = _carryType.Kind.Id switch
            {
                CarryTypeKind.CourierId => AddressHelper.GetFullAddress(
                    _order.DeliveryData.House,
                    _order.DeliveryData.Flat,
                    _order.DeliveryData.Extra,
                    placeName),
                CarryTypeKind.PickupId => placeName,
                _ => throw new NotSupportedException($"{_carryType.Kind.Name} not supported")
            };

            await FetchCitiesAsync();

            Fio fio = new Fio(parameter.NpDocument.RecipientFullName);

            Description = parameter.NpDocument.Description;
            RecipientPhone = parameter.NpDocument.RecipientPhone;
            RecipientFirstName = fio.FirstName;
            RecipientLastName = fio.LastName;
            RecipientMiddleName = fio.MiddleName;

            Title = $"Редактирование НП ТТН {_order.PackageTtn}";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            NpDocumentUpdateDto dto = new NpDocumentUpdateDto()
            {
                RecipientMiddleName = RecipientMiddleName,
                RecipientFirstName = RecipientFirstName,
                RecipientLastName = RecipientLastName,
                RecipientPhone = RecipientPhone,
                PackageWeight = (double)PackageWeight,
                PackagePlaces = (int)PackagePlaces,
                DeliveryData = DeliveryData,
                Description = Description,
                CityId = CityId!.Value
            };

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateNpTtn(_order.PackageTtn, dto)),
                "сохранении ТТН",
                "ТТН сохранена",
                this,
                true,
                showNotification: true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private async Task FetchCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities = cities
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void SelectNpWarehouse()
        {
            if (CityId == null)
            {
                return;
            }

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                _carryType.Id,
                CityId.Value,
                RecipientAddress,
                DeliveryData);

            if (_carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = viewModel.FullAddressString;
                }
            }
            else if (_carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = viewModel.Place.PlaceName;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void CityIdChanged()
        {
            DeliveryData = null;
            RecipientAddress = null;
        }
    }
}