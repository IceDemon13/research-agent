using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.City.Actions;
using Telemart.Client.Data.Requests.Features.District;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Ukrposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Cities
{
    public sealed class CityViewModel : TelemartEditorViewModelBase<CityDto, CityParameter, CityViewItem>
    {
        private IReadOnlyCollection<DistrictDto> allDistricts;
        private IReadOnlyCollection<NpAreaDto> npAreas;
        private IReadOnlyCollection<MeDistrictDto> meDistricts;
        private IReadOnlyCollection<MeAreaDto> meAreas;
        private IReadOnlyCollection<UpDistrictDto> upDistricts;

        public CityViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            SetNpCityCommand = new DelegateCommand(SetNpCity, () => IsLockedByCurrentEmployee);
            SetMeCityCommand = new DelegateCommand(SetMeCity, () => IsLockedByCurrentEmployee);
            SetUpCityCommand = new DelegateCommand(SetUpCity, () => IsLockedByCurrentEmployee);
            SetUklonCityCommand = new DelegateCommand(SetUklonCity, () => IsLockedByCurrentEmployee);

            AddCityCarryCommand = new DelegateCommand(AddCityCarry, () => IsNewOrIsLockedByCurrentEmployee);
            DeleteCityCarryCommand = new DelegateCommand(DeleteCityCarry, () => SelectedCityCarry != null && IsNewOrIsLockedByCurrentEmployee);
        }

        public CityViewModel()
        {
        }

        public IDelegateCommand SetNpCityCommand { get; }

        public IDelegateCommand SetMeCityCommand { get; }

        public IDelegateCommand SetUpCityCommand { get; }

        public IDelegateCommand SetUklonCityCommand { get; }

        public IDelegateCommand AddCityCarryCommand { get; }

        public IDelegateCommand DeleteCityCarryCommand { get; }

        public CityCarryViewItem SelectedCityCarry
        {
            get { return GetProperty(() => SelectedCityCarry); }
            set { SetProperty(() => SelectedCityCarry, value); }
        }

        public ReadOnlyCollection<ComboBoxItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyCollection<ComboBoxItem> ActiveValues
        {
            get { return GetProperty(() => ActiveValues); }
            private set { SetProperty(() => ActiveValues, value); }
        }

        public ReadOnlyCollection<DeliveryServiceCity> NpCitites
        {
            get { return GetProperty(() => NpCitites); }
            private set { SetProperty(() => NpCitites, value); }
        }

        public ReadOnlyCollection<DeliveryServiceCity> UpCitites
        {
            get { return GetProperty(() => UpCitites); }
            private set { SetProperty(() => UpCitites, value); }
        }

        public ReadOnlyCollection<DeliveryServiceCity> UklonCities
        {
            get { return GetProperty(() => UklonCities); }
            private set { SetProperty(() => UklonCities, value); }
        }

        public ReadOnlyCollection<DeliveryServiceCity> MeCities
        {
            get { return GetProperty(() => MeCities); }
            private set { SetProperty(() => MeCities, value); }
        }

        public ReadOnlyCollection<AreaDto> Areas
        {
            get { return GetProperty(() => Areas); }
            private set { SetProperty(() => Areas, value); }
        }

        public ReadOnlyCollection<DistrictDto> Districts
        {
            get { return GetProperty(() => Districts); }
            private set { SetProperty(() => Districts, value); }
        }

        public bool IsEnableDeliveryCities
        {
            get { return GetProperty(() => IsEnableDeliveryCities); }
            private set { SetProperty(() => IsEnableDeliveryCities, value); }
        }

        protected override string CreatedActionMessage => throw new NotSupportedException();

        protected override string EntityName { get; } = "Город";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            ActiveValues = new[] { new ComboBoxItem(1, "Активен"), new ComboBoxItem(2, "Не активен") }.ToReadOnlyObservableCollection();
        }

        protected override Task<Result<CityDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(CityDto dto, MessageType messageType)
        {
            return new CityMessage(dto, messageType);
        }

        protected override Task<CityDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCity(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            ActiveValues = new[] { new ComboBoxItem(1, "Активен"), new ComboBoxItem(2, "Не активен") }.ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshNpCitiesAsync(),
                RefreshUpCitiesAsync(),
                RefreshUklonCitiesAsync(),
                RefreshMeCitiesAsync(),
                RefreshEmployeesAsync(),
                RefreshAreaAsync(),
                GetDistrictsAsync(),
                GetAreasAsync());

            await base.HandleLoadedAsync();
        }

        protected override void AfterSetData()
        {
            base.AfterSetData();

            Carries = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Active || Model.CityCarries?.Select(z => z.CarryId).Contains(x.Id) == true)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            SetDistricts();
        }

        protected override Task<LockResponse<CityDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockCity(id));
        }

        protected override Task<LockResponse<CityDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockCity(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"{Model.Name} ({Model.Id})";
        }

        protected override Task<Result<CityDto>> UpdateEntityAsync()
        {
            CitySaveDto saveDto = new(
                Model.Id,
                Model.Name,
                Model.NameUkr,
                Model.NameEn,
                Model.NpCityRef,
                Model.MeCityRef,
                Model.UpCityId,
                Model.UklonCityId,
                Model.Position,
                Model.AreaId,
                Model.DistrictId,
                Model.CityCarries.Select(x => new CityCarryDto(x.Id, x.CarryId, x.AvailOnWeb, x.CreatedBy)).ToList());

            return WebClient.ExecuteApiRequestAsync(new UpdateCity(saveDto));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Model.AreaId):
                    SetDistricts();
                    break;
            }
        }

        private void AddCityCarry()
        {
            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(Carries, "Выбор доставки", "Доставка"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.CityCarries.Any(x => x.CarryId == viewModel.SelectedItem!.Value.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Такая доставка уже добавлена");
                return;
            }

            Model.CityCarries.Add(new CityCarryViewItem(viewModel.SelectedItem!.Value.Id, true, WebClient.AuthenticatedEmployee.Id));
        }

        private void DeleteCityCarry()
        {
            Model.CityCarries.Remove(SelectedCityCarry);
        }

        private async Task RefreshNpCitiesAsync()
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(CarryType.NpDeliveryId);
            IReadOnlyCollection<DeliveryServiceCity> npCities = await carryType.GetCityProvider().GetCitiesAsync(null);
            NpCitites = npCities.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private async Task RefreshUpCitiesAsync()
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(CarryType.UpDeliveryId);
            IReadOnlyCollection<DeliveryServiceCity> upCities = await carryType.GetCityProvider().GetCitiesAsync(null);
            UpCitites = upCities.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private async Task RefreshUklonCitiesAsync()
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(CarryType.UklonId);

            IReadOnlyCollection<DeliveryServiceCity> upCities = await carryType.GetCityProvider().GetCitiesAsync(null);
            UklonCities = upCities.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private async Task RefreshMeCitiesAsync()
        {
            CarryType carryType = Dictionaries.GetItemById<CarryType>(CarryType.MeDeliveryId);
            IReadOnlyCollection<DeliveryServiceCity> meCities = await carryType.GetCityProvider().GetCitiesAsync(null);
            MeCities = meCities.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            PagedResult<EmployeeDto> employeesData = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            Employees = employeesData.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private void SetNpCity()
        {
            string areaRef = string.Empty;
            if (Model.AreaId.HasValue)
            {
                NpAreaDto areaNp = npAreas.FirstOrDefault(x => x.AreaId == Model.AreaId);

                if (areaNp is null)
                {
                    AreaDto areaDto = Areas.First(x => x.Id == Model.AreaId);

                    MessageFacadeService.ShowNotificationWarning($"В области {areaDto.Name} отсутствует сопоставление c Новой почтой");
                }
                else
                {
                    areaRef = areaNp.Ref;
                }
            }

            DeliveryServiceCity city = GetDeliveryServiceCity(Dictionaries.GetItemById<CarryType>(CarryType.NpDeliveryId), null, areaRef);

            if (city != null)
            {
                Model.NpCityRef = city.CityRef;
            }
        }

        private void SetUklonCity()
        {
            DeliveryServiceCity city = GetDeliveryServiceCity(Dictionaries.GetItemById<CarryType>(CarryType.UklonId), null, null);

            if (city != null)
            {
                Model.UklonCityId = int.Parse(city.CityRef);
            }
        }

        private void SetMeCity()
        {
            string districtRef = string.Empty;

            string areaRef = string.Empty;

            if (Model.DistrictId.HasValue)
            {
                MeDistrictDto meDistrictDto = meDistricts.FirstOrDefault(x => x.DistrictId == Model.DistrictId);

                if (meDistrictDto is null)
                {
                    DistrictDto districtDto = Districts.First(x => x.Id == Model.DistrictId);

                    MessageFacadeService.ShowNotificationWarning($"В районе {districtDto.Name} отсутствует сопоставление c MeestExpress");
                }
                else
                {
                    districtRef = meDistrictDto.Ref;
                }
            }

            if (Model.AreaId.HasValue && string.IsNullOrEmpty(districtRef))
            {
                MeAreaDto area = meAreas.FirstOrDefault(x => x.AreaId == Model.AreaId);

                if (area is null)
                {
                    AreaDto areaDto = Areas.First(x => x.Id == Model.AreaId);

                    MessageFacadeService.ShowNotificationWarning($"В области {areaDto.Name} отсутствует сопоставление c MeestExpress");
                }
                else
                {
                    areaRef = area.Ref;
                }
            }

            DeliveryServiceCity city = GetDeliveryServiceCity(Dictionaries.GetItemById<CarryType>(CarryType.MeDeliveryId),  districtRef, areaRef);

            if (city != null)
            {
                Model.MeCityRef = city.CityRef;
            }
        }

        private void SetUpCity()
        {
            if (Model.DistrictId.HasValue)
            {
                UpDistrictDto upDistrictDto = upDistricts.FirstOrDefault(x => x.DistrictId == Model.DistrictId);

                if (upDistrictDto is null)
                {
                    DistrictDto districtDto = Districts.First(x => x.Id == Model.DistrictId);

                    MessageFacadeService.ShowNotificationWarning($"В районе {districtDto.Name} отсутствует сопоставление c Укрпочтой");
                }
                else
                {
                    DeliveryServiceCity city = GetDeliveryServiceCity(Dictionaries.GetItemById<CarryType>(CarryType.UpDeliveryId), upDistrictDto.Id.ToString(), null);

                    if (city != null)
                    {
                        if (int.TryParse(city.CityRef, out int upCityId))
                        {
                            Model.UpCityId = upCityId;
                        }
                        else
                        {
                            MessageFacadeService.ShowNotificationError("Не удалось преобразовать идентификатор города");
                        }
                    }
                }
            }
        }

        private DeliveryServiceCity GetDeliveryServiceCity(CarryType carry, string districtRef, string areaRef)
        {
            CityParameter parameter = new CityParameter(Model.Id, carry, districtRef, areaRef);

            SelectDeliveryServiceCityViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryServiceCityViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return null;
            }

            if (!viewModel.SelectedCity.Active)
            {
                MessageFacadeService.ShowNotificationWarning("Город на активен");
                return null;
            }

            return viewModel.SelectedCity;
        }

        private async Task RefreshAreaAsync()
        {
            List<AreaDto> areas = await WebClient.ExecuteApiRequestAsync(new QueryAreas());

            Areas = areas.OrderBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task GetDistrictsAsync()
        {
            allDistricts = await WebClient.ExecuteApiRequestAsync(new QueryDistricts());

            meDistricts = await WebClient.ExecuteApiRequestAsync(new QueryMeDistricts());

            upDistricts = await WebClient.ExecuteApiRequestAsync(new QueryUpDistricts());
        }

        private async Task GetAreasAsync()
        {
            npAreas = await WebClient.ExecuteApiRequestAsync(new QueryNpAreas());

            meAreas = await WebClient.ExecuteApiRequestAsync(new QueryMeAreas());
        }

        private void SetDistricts()
        {
            if (Model.AreaId.HasValue)
            {
                Districts = allDistricts.Where(x => x.AreaId == Model.AreaId).OrderBy(x => x.Name).ToReadOnlyObservableCollection();
                IsEnableDeliveryCities = true;
            }
            else
            {
                Districts = Array.Empty<DistrictDto>().ToReadOnlyObservableCollection();
                Model.DistrictId = null;
                IsEnableDeliveryCities = false;
            }
        }
    }
}