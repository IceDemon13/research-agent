using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Ukrposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class SelectDeliveryAddressViewModel : TelemartDialogViewModelBase
    {
        private List<UpHouseDto> _upHouseDtos;

        public SelectDeliveryAddressViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;

            HandleLoadHousesByStreetCommand = new AsyncCommand(LoadHousesByStreetAsync, () => VisibleIndex);
            HandleDefinitionIndexCommand = new DelegateCommand(DefinitionIndex, () => VisibleIndex);
        }

        public SelectDeliveryAddressViewModel()
        {
        }

        public ReadOnlyObservableCollection<DeliveryServicePlaceDto> Places
        {
            get { return GetProperty(() => Places); }
            private set { SetProperty(() => Places, value); }
        }

        public DeliveryServicePlaceDto Place
        {
            get { return GetProperty(() => Place); }
            set { SetProperty(() => Place, value, () => RaisePropertyChanged(nameof(FullAddressString))); }
        }

        public string House
        {
            get { return GetProperty(() => House); }
            set { SetProperty(() => House, value, () => RaisePropertyChanged(nameof(FullAddressString))); }
        }

        public string Index
        {
            get { return GetProperty(() => Index); }
            set { SetProperty(() => Index, value); }
        }

        public string FlatNumber
        {
            get { return GetProperty(() => FlatNumber); }
            set { SetProperty(() => FlatNumber, value, () => RaisePropertyChanged(nameof(FullAddressString))); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value, () => RaisePropertyChanged(nameof(FullAddressString))); }
        }

        public string OldAddressValue
        {
            get { return GetProperty(() => OldAddressValue); }
            private set { SetProperty(() => OldAddressValue, value); }
        }

        public bool SelectNpAddressForWarehouse
        {
            get { return GetProperty(() => SelectNpAddressForWarehouse); }
            private set { SetProperty(() => SelectNpAddressForWarehouse, value); }
        }

        public bool VisibleIndex
        {
            get { return GetProperty(() => VisibleIndex); }
            set { SetProperty(() => VisibleIndex, value, () => RaisePropertyChanged(nameof(Index))); }
        }

        public IAsyncCommand HandleLoadHousesByStreetCommand { get; }

        public IDelegateCommand HandleDefinitionIndexCommand { get; }

        public string FullAddressString => AddressHelper.GetFullAddress(House, FlatNumber, Comment, Place?.PlaceName);

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<SelectDeliveryAddressViewModel> builder)
        {
            builder.Property(x => x.Place).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.House).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.FullAddressString).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Index).MatchesInstanceRule((x, y) => !y.VisibleIndex || !string.IsNullOrEmpty(x), () => "Индекс обязателен");
        }

        public DeliveryDataDto GetDeliveryServiceData()
        {
            return new DeliveryDataDto
            {
                CityId = Place.CityId,
                PlaceId = Place.PlaceId,
                Street = Place.PlaceName,
                House = House,
                Flat = FlatNumber,
                Extra = Comment,
                MaxAllowedWeight = null,
                Address = Place.PlaceName,
                AddressUkr = Place.PlaceNameUkr,
                AddressEn = Place.PlaceNameEn,
                Index = Index
            };
        }

        protected override async Task HandleLoadedAsync()
        {
            SelectDeliveryDataParameter parameter = (SelectDeliveryDataParameter)Parameter;

            VisibleIndex = parameter.CarryId == CarryType.UpDeliveryId;

            List<DeliveryServicePlaceDto> places = await WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(parameter.CarryId, parameter.CityId));

            Places = places.ToReadOnlyObservableCollection();

            OldAddressValue = parameter.AddressOld;
            SelectNpAddressForWarehouse = parameter.SelectNpAddressForWarehouse;

            if (parameter.DeliveryDataOld != null)
            {
                Place = Places.FirstOrDefault(x => string.Equals(x.PlaceId, parameter.DeliveryDataOld.PlaceId, StringComparison.OrdinalIgnoreCase));
                House = parameter.DeliveryDataOld.House;
                FlatNumber = parameter.DeliveryDataOld.Flat;
                Comment = parameter.DeliveryDataOld.Extra;
                Index = parameter.DeliveryDataOld.Index;
            }

            Title = "Выберите адрес";
        }

        protected override Task HandleOkAsync()
        {
            string[] errors = Validate().ToArray();

            if (errors.Any())
            {
                MessageFacadeService.ShowNotificationWarning(string.Join(Environment.NewLine, errors));
            }
            else
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }

        private IEnumerable<string> Validate()
        {
            Regex regex = new Regex(@"^[0-9]+[\:\-]*[А-Яа-я]{0,1}$"); // 12а || 12

            string[] parts = House.Split('/');

            if (parts.Length > 2 || parts.Any(x => !regex.IsMatch(x)))
            {
                yield return "Поле дом заполнено некорректно";
            }
        }

        private async Task LoadHousesByStreetAsync()
        {
            if (VisibleIndex)
            {
                Index = null;

                if (Place != null && int.TryParse(Place.PlaceId, out int upStreetId))
                {
                    Result<List<UpHouseDto>> result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new QueryUpHouses(upStreetId)),
                        "получении домов по улице",
                        null,
                        this,
                        true,
                        showNotification: false);

                    if (result.IsSuccess && result.Data?.Count > 0)
                    {
                        _upHouseDtos = result.Data;
                    }
                }
            }
        }

        private void DefinitionIndex()
        {
            if (VisibleIndex && !string.IsNullOrEmpty(House))
            {
                UpHouseDto houseDto = _upHouseDtos?.FirstOrDefault(x => string.Compare(x.HouseNumber, House.Trim(), StringComparison.OrdinalIgnoreCase) == 0);

                if (houseDto != null)
                {
                    Index = houseDto.Index;
                }
                else
                {
                    string houmeNumberWithoutLetter = new string(new string(House
                        .SkipWhile(x => !char.IsDigit(x))
                        .TakeWhile(char.IsDigit)
                        .ToArray()));

                    if (int.TryParse(houmeNumberWithoutLetter, out int houseNum))
                    {
                        int num = houseNum - 1 > 0 ? houseNum - 1 : houseNum;

                        for (int i = num;  i <= houseNum + 1; i++)
                        {
                            UpHouseDto nearestHouse = _upHouseDtos?.FirstOrDefault(x => x.HouseNumber.Contains(i.ToString()));

                            if (nearestHouse != null)
                            {
                                Index = nearestHouse.Index;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(Index))
                {
                    MessageFacadeService.ShowNotificationWarning($"Индекс не определился.{Environment.NewLine}Уточните индекс у клиента");
                }
            }
            else
            {
                Index = null;
            }
        }
    }
}