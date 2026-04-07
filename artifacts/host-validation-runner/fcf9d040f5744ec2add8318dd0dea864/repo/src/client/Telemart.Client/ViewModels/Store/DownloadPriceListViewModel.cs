using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class DownloadPriceListViewModel : TelemartDialogViewModelBase
    {
        public DownloadPriceListViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
        }

        public DownloadPriceListViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public int? SelectedContractorId
        {
            get { return GetProperty(() => SelectedContractorId); }
            set { SetProperty(() => SelectedContractorId, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>();

        private SaveFileDialogService SaveFileDialogService => (SaveFileDialogService)GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        public static void BuildMetadata(MetadataBuilder<DownloadPriceListViewModel> builder)
        {
            builder.Property(x => x.SelectedContractorId).Required();
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshContractorsAsync(), RefreshCategoriesAsync());

            Title = "Прайс-лист";

            async Task RefreshContractorsAsync()
            {
                IReadOnlyCollection<int> allowSubdivisions = WebClient.AuthenticatedEmployee.AllowSubdivisions;

                PagedResult<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);

                Contractors = contractors.Data
                    .Where(x => !x.IsFolder && x.IsClient && x.Active && allowSubdivisions.Contains(x.SubdivisionId))
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshCategoriesAsync()
            {
                PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

                ObservableCollection<CategoryViewItem> data = categories.Data
                    .Where(x => x.ParentLevel <= 0 && x.Active > 0)
                    .OrderBy(x => x.Left)
                    .Select(x => Mapper.Map<CategoryViewItem>(x))
                    .ToObservableCollection();

                foreach (CategoryViewItem item in data)
                {
                    item.Selected = true;
                }

                Categories = data;
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedContractorId == null)
            {
                return;
            }

            CurrentWindowService.ActualWindow.Closing += ActualWindowOnClosing;

            try
            {
                QueryPriceList request = new QueryPriceList(
                    SelectedContractorId.Value,
                    Categories.Where(x => x.Selected == true).Select(x => x.Id).ToArray());

                byte[] response = await WebClient.ExecuteCatalogApiRequestAsBytesAsync(request);

                string fileName = $"pricelist_{DateTime.Now.ToString("yyyy-MM-dd")}";
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                SaveFileDialogService.DefaultFileName = fileName;
                SaveFileDialogService.InitialDirectory = folderPath;

                if (SaveFileDialogService.ShowDialog())
                {
                    string filePath = SaveFileDialogService.GetFullFileName();
                    await File.WriteAllBytesAsync(filePath, response);
                    MessageFacadeService.ShowNotificationInfo("Прайс-лист сохранен успешно");
                }

                IsOk = true;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обработке прайс-листа");
                Logger.LogError(exception, "Error processing price list");
            }
            finally
            {
                CurrentWindowService.ActualWindow.Closing -= ActualWindowOnClosing;
            }

            if (IsOk)
            {
                Close();
            }
        }

        private void ActualWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
