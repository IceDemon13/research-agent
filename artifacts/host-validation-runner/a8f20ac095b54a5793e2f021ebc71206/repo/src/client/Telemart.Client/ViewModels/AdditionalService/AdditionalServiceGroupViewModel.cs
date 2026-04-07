using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalService.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class AdditionalServiceGroupViewModel : TelemartEditorViewModelBase<AdditionalServiceGroupDto, AdditionalServiceGroupParameter, AdditionalServiceGroupSimpleViewItem>
    {
        public AdditionalServiceGroupViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public AdditionalServiceGroupViewModel()
        {
        }

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Группа услуг";

        protected override string UpdatedActionMessage => "созранена";

        protected override void SetCreateTitle()
        {
            Title = "Создание группы услуг";
        }

        protected override void SetEditTitle()
        {
            Title = "Группа услуг";
        }

        protected override Task<Result<AdditionalServiceGroupDto>> CreateEntityAsync()
        {
            AdditionalServiceGroupCreateDto createDto = new AdditionalServiceGroupCreateDto()
            {
                Name = Model.Name,
                NameUa = Model.NameUa,
                NameEn = Model.NameEn,
                Description = Model.Description,
                DescriptionUa = Model.DescriptionUa,
                DescriptionEn = Model.DescriptionEn,
                ParentId = Model.ParentId,
                MultiSelect = Model.MultiSelect
            };

            CreateAdditionalServiceGroup gatewayRequest = new CreateAdditionalServiceGroup(createDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<AdditionalServiceGroupDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceGroup(id));
        }

        protected override Task<LockResponse<AdditionalServiceGroupDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAdditionalServiceGroup(id));
        }

        protected override Task<LockResponse<AdditionalServiceGroupDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAdditionalServiceGroup(id));
        }

        protected override Task<Result<AdditionalServiceGroupDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateAdditionalServiceGroup(Model.Id, Model.Name, Model.NameUa, Model.NameEn, Model.Description, Model.DescriptionUa, Model.DescriptionEn, Model.MultiSelect));
        }
    }
}