using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssemblyTest;
using Telemart.Client.Data.Requests.Features.AssemblyTest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestGroupViewModel : TelemartEditorViewModelBase<AssemblyTestGroupDto, AssemblyTestGroupParameter, AssemblyTestGroupSimpleViewItem>
    {
        public AssemblyTestGroupViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService, IMapper mapper, IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public AssemblyTestGroupViewModel()
        {
        }

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Группа тестов";

        protected override string UpdatedActionMessage => "сохранена";

        protected override Task<Result<AssemblyTestGroupDto>> CreateEntityAsync()
        {
            AssemblyTestGroupCreateDto createDto = new AssemblyTestGroupCreateDto()
            {
                Name = Model.Name,
                NameUa = Model.NameUa,
                NameEn = Model.NameEn,
                ParentId = Model.ParentId
            };

            CreateAssemblyTestGroup gatewayRequest = new CreateAssemblyTestGroup(createDto);

            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<AssemblyTestGroupDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAssemblyTestGroup(id));
        }

        protected override Task<LockResponse<AssemblyTestGroupDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAssemblyTestGroup(id));
        }

        protected override Task<LockResponse<AssemblyTestGroupDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAssemblyTestGroup(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание группы тестов";
        }

        protected override void SetEditTitle()
        {
            Title = $"Группа тестов №{Model.Id}";
        }

        protected override Task<Result<AssemblyTestGroupDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateAssemblyTestGroup(Model.Id, Model.Name, Model.NameUa, Model.NameEn));
        }
    }
}