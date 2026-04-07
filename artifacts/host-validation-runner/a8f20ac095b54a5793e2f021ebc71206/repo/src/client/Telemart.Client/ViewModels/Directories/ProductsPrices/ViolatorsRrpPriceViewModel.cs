using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using OfficeOpenXml;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ViolatorsRrpPriceViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyCollection<ViolatorsRrpProduct> products;

        public ViolatorsRrpPriceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ViolatorsRrpPriceViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Competitiors
        {
            get { return GetProperty(() => Competitiors); }
            private set { SetProperty(() => Competitiors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> RrpSuppliers
        {
            get { return GetProperty(() => RrpSuppliers); }
            private set { SetProperty(() => RrpSuppliers, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedCompetitiors
        {
            get { return GetProperty(() => SelectedCompetitiors); }
            private set { SetProperty(() => SelectedCompetitiors, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedRrpSuppliers
        {
            get { return GetProperty(() => SelectedRrpSuppliers); }
            private set { SetProperty(() => SelectedRrpSuppliers, value); }
        }

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>();

        protected override Task HandleLoadedAsync()
        {
            ViolatorsRrpPriceParameter parameter = (ViolatorsRrpPriceParameter)Parameter;

            products = parameter.ProductPrices;

            Competitiors = parameter.Competitors.ToReadOnlyObservableCollection();
            RrpSuppliers = parameter.RrpSuppliers.ToReadOnlyObservableCollection();

            SelectedCompetitiors = parameter.Competitors.ToObservableCollection();
            SelectedRrpSuppliers = parameter.RrpSuppliers.ToObservableCollection();

            Title = "Параметры";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            string fileName = "rrp_report";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            HashSet<int> selectedRrp = SelectedRrpSuppliers.Select(x => x.Id).ToHashSet();
            HashSet<int> selectedCompetitors = SelectedCompetitiors.Select(x => x.Id).ToHashSet();

            IReadOnlyCollection<ViolatorsRrpProductExcelItem> productPrices = products
                .Select(x => new
                {
                    ProductName = x.Name,
                    MinRrpPrice = x.RrpPrices
                        .Where(y => selectedRrp.Contains(y.Id))
                        .Select(y => y.Price)
                        .DefaultIfEmpty(0)
                        .Min(),
                    CompetitiorPrices = x.CompetitorPrices
                        .Where(y => selectedCompetitors.Contains(y.Id))
                        .DistinctBy(z => z.Id)
                        .ToDictionary(y => y.Id, z => z.Price)
                })
                .Where(x => x.MinRrpPrice > 0 && x.CompetitiorPrices.Values.Any(y => y < x.MinRrpPrice))
                .Select(x => new ViolatorsRrpProductExcelItem(x.ProductName, x.CompetitiorPrices, x.MinRrpPrice))
                .ToArray();

            if (!productPrices.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Все выбранные конкуренты соблюдают РРЦ");
            }
            else
            {
                SaveFileDialogService.ShowDialog(
                    args =>
                    {
                        File.WriteAllBytes(
                            SaveFileDialogService.File.GetFullName(),
                            GetExcelFileData(SelectedCompetitiors, productPrices));

                        MessageFacadeService.ShowNotificationInfo("Файл успешно сохранен");

                        IsOk = true;
                        Close();
                    },
                    folderPath,
                    fileName);
            }

            return Task.CompletedTask;
        }

        private byte[] GetExcelFileData(
            IReadOnlyCollection<ComboBoxItem> competitors,
            IReadOnlyCollection<ViolatorsRrpProductExcelItem> productPrices)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Нарушения");

                int row = 1;
                int col = 1;

                worksheet.Cells[row, col++].Value = "Товар";
                worksheet.Cells[row, col++].Value = "РРЦ";

                foreach (ComboBoxItem competitor in competitors)
                {
                    worksheet.Cells[row, col++].Value = competitor.DisplayValue;
                }

                worksheet.Cells[row, 1, row, col - 1].Style.Font.Bold = true;
                worksheet.View.FreezePanes(2, 1);

                row++;
                col = 1;

                foreach (ViolatorsRrpProductExcelItem product in productPrices)
                {
                    worksheet.Cells[row, col++].Value = product.ProductName;
                    worksheet.Cells[row, col++].Value = product.MinRrpPrice;

                    foreach (ComboBoxItem competitor in competitors)
                    {
                        if (product.CompetitorPrices.TryGetValue(competitor.Id, out decimal price)
                            && price < product.MinRrpPrice)
                        {
                            worksheet.Cells[row, col].Value = price;
                        }

                        col++;
                    }

                    row++;
                    col = 1;
                }

                worksheet.Cells.AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
    }
}