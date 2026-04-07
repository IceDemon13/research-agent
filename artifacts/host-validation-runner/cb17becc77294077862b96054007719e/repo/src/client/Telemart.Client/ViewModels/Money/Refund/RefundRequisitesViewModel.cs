using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public sealed class RefundRequisitesViewModel : TelemartDialogViewModelBase
    {
        private int _documentId;

        public RefundRequisitesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            private set { SetProperty(() => Requisites, value); }
        }

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            RefundRequisitesParameter parameter = (RefundRequisitesParameter)Parameter;

            Requisites = parameter.Requisites;

            _documentId = parameter.DocumentId;

            Title = "Заполнение реквизитов";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            RefundUpdateRequisitesMessage message = new RefundUpdateRequisitesMessage(
                Requisites.FirstName,
                Requisites.LastName,
                Requisites.MiddleName,
                Requisites.Inn,
                Requisites.Iban,
                Requisites.CardNumber,
                _documentId);

            Messenger.Send(message);

            CloseOk();

            return Task.CompletedTask;
        }
    }
}