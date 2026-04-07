using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPricesDebugViewModel : TelemartDialogViewModelBase
    {
        public ProductPricesDebugViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ProductPricesDebugViewModel()
        {
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }

        public ObservableRangeCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            private set { SetProperty(() => ValidationItems, value); }
        }

        #region DialogSettings

        public override int Height => 576;

        public override int MinHeight => 300;

        public override int MinWidth => 400;

        public override int Width => 800;

        #endregion

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Text = "Some debug info";
            ValidationItems = new ObservableRangeCollection<ValidationResultItem>
            {
                new ValidationResultItem("Error1", true)
            };
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            object[] parameters = (object[])parameter;

            string debugText = (string)parameters[0];
            IReadOnlyCollection<string> errors = (IReadOnlyCollection<string>)parameters[1];

            Text = debugText;
            ValidationItems = errors.Select(x => new ValidationResultItem(x, true)).ToObservableRangeCollection();
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Debug";

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