using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store
{
    public class ProductBarcodesDeleteViewModel : TelemartDialogViewModelBase
    {
        public ProductBarcodesDeleteViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Удаление штрих-кодов";
        }

        public ObservableCollection<string> Barcodes
        {
            get { return GetProperty(() => Barcodes); }
            set { SetProperty(() => Barcodes, value); }
        }

        public string SelectedBarcode
        {
            get { return GetProperty(() => SelectedBarcode); }
            set { SetProperty(() => SelectedBarcode, value); }
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (parameter is ICollection<string> barcodes)
            {
                Barcodes = new ObservableCollection<string>(barcodes);
            }

            base.OnParameterChanged(parameter);
        }

        protected override Task HandleOkAsync()
        {
            ProductSelectionValidationViewModel viewModel = DialogDocumentManagerService.ShowView<ProductSelectionValidationViewModel>(
                new ProductSelectionValidationViewModelParameter(SelectedBarcode, null, null),
                this);

            if (viewModel.IsOk)
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }

        protected override bool CanOk()
        {
            return SelectedBarcode != null;
        }
    }
}
