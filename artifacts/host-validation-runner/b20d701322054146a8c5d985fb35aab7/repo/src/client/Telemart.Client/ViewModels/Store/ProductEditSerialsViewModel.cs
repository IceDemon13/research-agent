using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class ProductEditSerialsViewModel : TelemartDialogViewModelBase
    {
        public ProductEditSerialsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DeleteCommand = new DelegateCommand<string>(DeleteSerialNumber, x => x != null && !ReadOnlySerials);
            ClearCommand = new DelegateCommand(ClearSerials, () => !ReadOnlySerials);
        }

        public ProductEditSerialsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand DeleteCommand { get; }

        public IDelegateCommand ClearCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            private set { SetProperty(() => SerialNumbers, value); }
        }

        public bool ReadOnlySerials
        {
            get { return GetProperty(() => ReadOnlySerials); }
            private set { SetProperty(() => ReadOnlySerials, value); }
        }

        public string SelectedSerialNumber
        {
            get { return GetProperty(() => SelectedSerialNumber); }
            set { SetProperty(() => SelectedSerialNumber, value); }
        }

        #endregion

        protected override Task HandleLoadedAsync()
        {
            ProductEditSerialsParameter parameter = (ProductEditSerialsParameter)Parameter;

            SerialNumbers = new ObservableCollection<string>(parameter.SerialNumbers);
            ReadOnlySerials = parameter.ReadOnly;

            Title = "Просмотр SN";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void ClearSerials()
        {
            if (MessageFacadeService.Confirm("Очистить все SN?"))
            {
                SerialNumbers.Clear();
            }
        }

        private void DeleteSerialNumber(string serialNumber)
        {
            SerialNumbers.Remove(serialNumber);
        }
    }
}