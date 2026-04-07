using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class PromoCodeConfirmationViewModel : TelemartDialogViewModelBase
    {
        public PromoCodeConfirmationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public PromoCodeConfirmationViewModel()
        {
        }

        #region INPC

        public ObservableCollection<PromoCodeProductViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 455;

        public override int MaxHeight => 600;

        public override int MaxWidth => 1000;

        public override int MinHeight => 200;

        public override int MinWidth => 600;

        public override int Width => 820;

        #endregion

        protected override Task HandleLoadedAsync()
        {
            IEnumerable<PromoCodeProductViewItem> items = (IEnumerable<PromoCodeProductViewItem>)Parameter;
            Items = new ObservableCollection<PromoCodeProductViewItem>(items);

            Title = "Результат";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }
    }
}