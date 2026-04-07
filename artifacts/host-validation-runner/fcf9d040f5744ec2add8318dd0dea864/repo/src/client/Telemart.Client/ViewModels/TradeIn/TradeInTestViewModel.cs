using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInTestViewModel : TelemartDialogViewModelBase
    {
        private bool _tested;

        public TradeInTestViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public bool Tested
        {
            get { return GetProperty(() => Tested); }
            set { SetProperty(() => Tested, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            TradeInTestParameter parameter = (TradeInTestParameter)Parameter;

            Tested = parameter.Tested;
            _tested = Tested;

            Title = "Протестировать";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (_tested == Tested)
            {
                MessageFacadeService.ShowNotificationError("Нечего сохранять");
                return Task.CompletedTask;
            }

            CloseOk();

            return Task.CompletedTask;
        }
    }
}