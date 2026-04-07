using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class GeneratedAssembledComputerRuleViewModel : TelemartDialogViewModelBase
    {
        public GeneratedAssembledComputerRuleViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Сгенерированная конфигурация";
        }

        public GeneratedAssembledComputerRuleViewModel()
        {
        }

        public IReadOnlyCollection<GeneratedAssembledComputerRuleProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            Products = ((GeneratedAssembledComputerRuleParameter)parameter).Products;
        }
    }
}