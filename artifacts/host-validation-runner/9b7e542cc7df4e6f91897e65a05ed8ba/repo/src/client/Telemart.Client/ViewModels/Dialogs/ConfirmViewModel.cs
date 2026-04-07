using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class ConfirmViewModel : TelemartDialogViewModelBase
    {
        public ConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            ConfirmViewModelParameter parameter = (ConfirmViewModelParameter)Parameter;

            Text = parameter.Text;
            Title = parameter.Title;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;

            Close();

            return Task.CompletedTask;
        }
    }
}