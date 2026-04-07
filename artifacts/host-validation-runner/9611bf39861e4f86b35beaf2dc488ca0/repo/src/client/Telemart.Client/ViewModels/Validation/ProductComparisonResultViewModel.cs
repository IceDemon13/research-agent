using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ProductComparisonResultViewModel : TelemartDialogViewModelBase
    {
        public ProductComparisonResultViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ExportToXlsxCommand = new DelegateCommand<TableView>(ExportToXlsx);
        }

        public ProductComparisonResultViewModel()
        {
        }

        public ObservableCollection<ProductComparisonResult> Items
        {
            get { return GetProperty(() => Items); }
            set { SetProperty(() => Items, value); }
        }

        public bool AllowContinue
        {
            get { return GetProperty(() => AllowContinue); }
            private set { SetProperty(() => AllowContinue, value); }
        }

        public bool VisibleProductId => Items?.Any(x => x.ProductId.HasValue) == true;

        public IDelegateCommand ExportToXlsxCommand { get; }

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        protected override Task HandleLoadedAsync()
        {
            Title = "Отклонение от плана";
            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            object[] p = (object[])parameter;

            IReadOnlyCollection<ProductComparisonResult> items = (IReadOnlyCollection<ProductComparisonResult>)p[0];
            AllowContinue = (bool)p[1];

            Items = new ObservableCollection<ProductComparisonResult>(items.OrderBy(x => x.ProductName));

            RaisePropertyChanged(nameof(VisibleProductId));
        }

        private void ExportToXlsx(TableView tableView)
        {
            string fileName = string.Empty;
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                x =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));
                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                folderPath,
                fileName);
        }
    }
}