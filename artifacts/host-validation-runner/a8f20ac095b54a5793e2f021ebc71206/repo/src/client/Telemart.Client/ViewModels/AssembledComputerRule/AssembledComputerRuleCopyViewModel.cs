using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleCopyViewModel : TelemartDialogViewModelBase
    {
        public AssembledComputerRuleCopyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Mapper = mapper;
        }

        public ObservableCollection<AssembledComputerRulesViewItem> AssembledComputerRules
        {
            get { return GetProperty(() => AssembledComputerRules); }
            set { SetProperty(() => AssembledComputerRules, value); }
        }

        public AssembledComputerRulesViewItem SelectedAssembledComputerRule
        {
            get { return GetProperty(() => SelectedAssembledComputerRule); }
            set { SetProperty(() => SelectedAssembledComputerRule, value); }
        }

        public bool IsCopyCategory
        {
            get { return GetProperty(() => IsCopyCategory); }
            set { SetProperty(() => IsCopyCategory, value); }
        }

        public bool IsCopyProduct
        {
            get { return GetProperty(() => IsCopyProduct); }
            set { SetProperty(() => IsCopyProduct, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            List<AssembledComputerRuleDto> assembledComputerRules = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRules()),
                "получении конфигураций",
                null,
                this,
                true,
                showNotification: false);

            if (assembledComputerRules != null)
            {
                AssembledComputerRules = assembledComputerRules
                    .Select(x => Mapper.Map<AssembledComputerRulesViewItem>(x))
                    .ToObservableCollection();
            }

            Title = "Копирование конфигурации";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        protected override bool CanOk()
        {
            return SelectedAssembledComputerRule != null && IsCopyCategory;
        }
    }
}