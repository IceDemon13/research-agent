using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class ProductSelectionViewModel : TelemartDialogViewModelBase
    {
        public ProductSelectionViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public ObservableCollection<Tuple<int, string>> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public Tuple<int, string> SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Выберите товар из списка";
            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedProduct != null)
            {
                IsOk = true;
                Close();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Выберите товар");
            }

            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            object[] p = (object[])parameter;

            IEnumerable<Tuple<int, string>> items = (IEnumerable<Tuple<int, string>>)p[0];
            int selectedId = (int)p[1];

            Products = items.ToObservableCollection();
            SelectedProduct = Products.FirstOrDefault(x => x.Item1 == selectedId);
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SelectedProduct != null)
            {
                OkCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
