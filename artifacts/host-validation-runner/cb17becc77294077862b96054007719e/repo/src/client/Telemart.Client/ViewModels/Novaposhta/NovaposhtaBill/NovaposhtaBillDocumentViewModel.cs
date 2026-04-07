using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaBill;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill
{
    public class NovaposhtaBillDocumentViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<string, string> npContractorNames;
        private IReadOnlyDictionary<int, string> employeeNames;
        private int billId;
        private string billNumber;

        public NovaposhtaBillDocumentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DeleteNpBillCommand = new AsyncCommand(DeleteNpBillAsync);
        }

        public IAsyncCommand DeleteNpBillCommand { get; }

        public IEnumerable<SummaryViewItem> SummaryViewItems
        {
            get { return GetProperty(() => SummaryViewItems); }
            private set { SetProperty(() => SummaryViewItems, value); }
        }

        private SaveFileDialogService SaveFileDialogService => (SaveFileDialogService)GetService<ISaveFileDialogService>();

        protected override async Task HandleLoadedAsync()
        {
            NovaposhtaBillDocumentParameter parameter = (NovaposhtaBillDocumentParameter)Parameter;

            await Task.WhenAll(RefreshContractors(), RefreshEmployees());

            NovaposhtaBillSimpleDto bill = await WebClient.ExecuteApiRequestAsync(new QueryNpBill(parameter.BillId));

            billId = bill.Id;
            billNumber = bill.Number;

            SummaryViewItems = GetSummaryItems(bill);

            Title = $"Счет №{bill.Number}";

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshContractors()
            {
                List<NpContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(), true);

                npContractorNames = contractors.ToDictionary(x => x.Id, y => y.Name);
            }
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                NovaposhtaBillDocumentDto billDocument = await WebClient.ExecuteApiRequestAsync(new QueryNpBillDocument(billId));

                string fileName = $"Novaposhta_Bill_{billNumber}";
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                SaveFileDialogService.DefaultFileName = fileName;
                SaveFileDialogService.InitialDirectory = folderPath;

                if (SaveFileDialogService.ShowDialog())
                {
                    await File.WriteAllBytesAsync(SaveFileDialogService.GetFullFileName(), billDocument.Data);

                    MessageFacadeService.ShowNotificationInfo("Документ успешно сохранен");
                    Close();
                }
            }
            catch
            {
                MessageFacadeService.ShowNotificationWarning("Файл не найден");
            }
        }

        private async Task DeleteNpBillAsync()
        {
            if (MessageFacadeService.Confirm("Вы уверены что ходите удалить счет?"))
            {
                try
                {
                    await WebClient.ExecuteApiRequestAsync(new DeleteNpBill(billId));
                    MessageFacadeService.ShowNotificationInfo("Счет успешно удален");
                    Close();
                }
                catch
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при удалении счета");
                }
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems(NovaposhtaBillSimpleDto dto)
        {
            yield return new SummaryViewItem("Номер", dto.Number);

            if (!string.IsNullOrEmpty(dto.ContractorRef))
            {
                yield return new SummaryViewItem("Контрагент", npContractorNames.GetValueOrDefault(dto.ContractorRef));
            }

            yield return new SummaryViewItem("Создан", $"{employeeNames.GetValueOrDefault(dto.CreatedBy)} ({dto.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменен", $"{employeeNames.GetValueOrDefault(dto.ModifiedBy)} ({dto.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (dto.InvoicedOn.HasValue)
            {
                yield return new SummaryViewItem("Выставлен", dto.InvoicedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            yield return new SummaryViewItem("Сумма", CurrencyFormatingRules.ToUahStr(dto.Amount));
        }
    }
}
