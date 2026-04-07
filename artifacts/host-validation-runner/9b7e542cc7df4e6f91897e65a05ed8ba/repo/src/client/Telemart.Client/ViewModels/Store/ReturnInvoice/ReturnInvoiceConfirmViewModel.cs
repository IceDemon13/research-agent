using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
   public sealed class ReturnInvoiceConfirmViewModel : TelemartDialogViewModelBase
    {
        public ReturnInvoiceConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ShowProductSerialsCommand = new DelegateCommand<ReturnInvoiceProductConfirmViewItem>(ShowProductSerials);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
        }

        public IDelegateCommand ShowProductSerialsCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public ObservableCollection<ReturnInvoiceProductConfirmViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            ReturnInvoiceProductConfirmParameter parameter = (ReturnInvoiceProductConfirmParameter)Parameter;

            Title = $"Согласовать возврат №{parameter.ReturnInvoiceId}";

            Products = parameter.Products;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void ShowProductSerials(ReturnInvoiceProductConfirmViewItem productViewItem)
        {
            if (productViewItem.SerialNumbers?.Any() == true)
            {
                DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(new ProductEditSerialsParameter(productViewItem.SerialNumbers, true), this);
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нет серийных номеров");
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            if (string.Equals(e.FieldName, nameof(ReturnInvoiceProductConfirmViewItem.ScannedQuantity), StringComparison.Ordinal))
            {
                ShowProductSerialsCommand.Execute((ReturnInvoiceProductConfirmViewItem)e.Data);
            }
        }
    }
}
