using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class QrCodeViewModel : TelemartDialogViewModelBase
    {
        public QrCodeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            QrCodeParameter parameter = (QrCodeParameter)Parameter;

            Url = parameter.Url;

            Title = parameter.Title;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}