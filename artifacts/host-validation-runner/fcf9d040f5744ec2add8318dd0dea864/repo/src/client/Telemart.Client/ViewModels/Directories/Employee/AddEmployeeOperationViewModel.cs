using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    internal sealed class AddEmployeeOperationViewModel : TelemartDialogViewModelBase
    {
        public AddEmployeeOperationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public AddEmployeeOperationViewModel()
        {
        }

        public ReadOnlyObservableCollection<OperationDto> AvailableEmployeeOperations
        {
            get { return GetProperty(() => AvailableEmployeeOperations); }
            private set { SetProperty(() => AvailableEmployeeOperations, value); }
        }

        public OperationDto SelectedOperation
        {
            get { return GetProperty(() => SelectedOperation); }
            set { SetProperty(() => SelectedOperation, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AddEmployeeOperationViewModel> buidler)
        {
            buidler.Property(x => x.SelectedOperation).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            OperationDto[] availOperations = (OperationDto[])Parameter;

            AvailableEmployeeOperations = availOperations.ToReadOnlyObservableCollection();

            Title = "Добавление операции";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}