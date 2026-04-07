using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.Requests.Features.District;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Client.TransferObjects.Ukrposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.District
{
    public sealed class DistrictViewModel : TelemartEditorViewModelBase<DistrictDto, DistrictParameter, DistrictViewItem>
    {
        private List<MeDistrictDto> allMeDistricts;
        private List<UpDistrictDto> allUpDistricts;

        public DistrictViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public ReadOnlyCollection<MeDistrictDto> MeDistricts
        {
            get { return GetProperty(() => MeDistricts); }
            set { SetProperty(() => MeDistricts, value); }
        }

        public ReadOnlyCollection<UpDistrictDto> UpDistricts
        {
            get { return GetProperty(() => UpDistricts); }
            set { SetProperty(() => UpDistricts, value); }
        }

        public ReadOnlyCollection<AreaDto> Areas
        {
            get { return GetProperty(() => Areas); }
            set { SetProperty(() => Areas, value); }
        }

        protected override string CreatedActionMessage => "создан";

        protected override string EntityName => "Район";

        protected override string UpdatedActionMessage => "сохранен";

        protected override async Task HandleLoadedAsync()
        {
            List<AreaDto> areas = await WebClient.ExecuteApiRequestAsync(new QueryAreas());

            allMeDistricts = await WebClient.ExecuteApiRequestAsync(new QueryMeDistricts());

            allUpDistricts = await WebClient.ExecuteApiRequestAsync(new QueryUpDistricts());

            Areas = areas.OrderBy(x => x.Name).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override Task<LockResponse<DistrictDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<DistrictDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание района";
        }

        protected override void SetEditTitle()
        {
            Title = "Редактирование района";
        }

        protected override Task<Result<DistrictDto>> UpdateEntityAsync()
        {
            DistrictDto district = new DistrictDto(Model.Id, Model.Name, Model.NameUkr, Model.NameEn, Model.Active, Model.AreaId.Value, Model.MeDistrictRef, Model.UpDistrictId);
            return WebClient.ExecuteApiRequestAsync(new UpdateDistrict(Model.Id, district));
        }

        protected override Task<Result<DistrictDto>> CreateEntityAsync()
        {
            CreateDistrict createDistrict = new CreateDistrict(Model.Name, Model.NameUkr, Model.NameEn, Model.Active, Model.AreaId.Value, Model.MeDistrictRef, Model.UpDistrictId);
            return WebClient.ExecuteApiRequestAsync(createDistrict);
        }

        protected override Task<DistrictDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryDistrict(id));
        }

        protected override object CreateEntityMessage(DistrictDto dto, MessageType messageType)
        {
            return new EntityMessage<DistrictDto>(dto, messageType);
        }

        protected override void AfterSetData()
        {
            SetDistricts();
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

        private void SetDistricts()
        {
            MeDistricts = allMeDistricts.Where(x => x.AreaId.HasValue && x.AreaId == Model.AreaId).OrderBy(x => x.NameUkr).ToReadOnlyObservableCollection();
            UpDistricts = allUpDistricts.Where(x => x.AreaId.HasValue && x.AreaId == Model.AreaId).OrderBy(x => x.NameUkr).ToReadOnlyObservableCollection();

            if (MeDistricts.Count == 0 && Model.AreaId.HasValue)
            {
                MessageFacadeService.ShowNotificationWarning("Отсутсвует соответсвие областей MeestExpress");
            }

            if (UpDistricts.Count == 0 && Model.AreaId.HasValue)
            {
                MessageFacadeService.ShowNotificationWarning("Отсутсвует соответсвие областей Укрпочты");
            }
        }
    }
}