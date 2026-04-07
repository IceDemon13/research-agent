using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Ukrposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Area
{
    public sealed class AreaViewModel : TelemartEditorViewModelBase<AreaDto, AreaParameter, AreaViewItem>
    {
        public AreaViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public ReadOnlyCollection<NpAreaDto> NpAreas
        {
            get { return GetProperty(() => NpAreas); }
            private set { SetProperty(() => NpAreas, value); }
        }

        public ReadOnlyCollection<MeAreaDto> MeAreas
        {
            get { return GetProperty(() => MeAreas); }
            private set { SetProperty(() => MeAreas, value); }
        }

        public ReadOnlyCollection<UpAreaDto> UpAreas
        {
            get { return GetProperty(() => UpAreas); }
            private set { SetProperty(() => UpAreas, value); }
        }

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Область";

        protected override string UpdatedActionMessage => "сохранена";

        protected override Task<Result<AreaDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override Task<AreaDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryArea(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            List<NpAreaDto> npAreas = await WebClient.ExecuteApiRequestAsync(new QueryNpAreas());
            List<MeAreaDto> meAreas = await WebClient.ExecuteApiRequestAsync(new QueryMeAreas());
            List<UpAreaDto> upAreas = await WebClient.ExecuteApiRequestAsync(new QueryUpAreas());

            NpAreas = npAreas.ToReadOnlyObservableCollection();
            MeAreas = meAreas.ToReadOnlyObservableCollection();
            UpAreas = upAreas.ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override Task<LockResponse<AreaDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<AreaDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = "Редактирование области";
        }

        protected override Task<Result<AreaDto>> UpdateEntityAsync()
        {
            AreaDto saveDto = new(Model.Id, Model.Name, Model.NameUkr, Model.NameEn, Model.Active, Model.NpAreaRef, Model.MeAreaRef, Model.UpAreaIds?.Cast<int>().ToList());

            return WebClient.ExecuteApiRequestAsync(new UpdateArea(Model.Id, saveDto));
        }
    }
}