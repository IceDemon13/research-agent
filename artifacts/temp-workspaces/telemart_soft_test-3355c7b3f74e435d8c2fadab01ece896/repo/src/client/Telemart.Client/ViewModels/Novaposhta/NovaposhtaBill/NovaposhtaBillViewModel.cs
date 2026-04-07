using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill
{
    public class NovaposhtaBillViewModel : TelemartDialogViewModelBase
    {
        public NovaposhtaBillViewModel(
            IExcelImportEngine<NovaposhtaBillTtnDto> excelImportEngine,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
        : base(webClient, dictionaries, messageFacadeService)
        {
            ExcelImportEngine = excelImportEngine;

            OpenFileCommand = new DelegateCommand(OpenFile);
        }

        public IDelegateCommand OpenFileCommand { get; }

        public string FilePath
        {
            get { return GetProperty(() => FilePath); }
            set { SetProperty(() => FilePath, value); }
        }

        public string Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public DateTime? DateInvoice
        {
            get { return GetProperty(() => DateInvoice); }
            set { SetProperty(() => DateInvoice, value); }
        }

        public ReadOnlyObservableCollection<NpContractorDto> NpContractors
        {
            get { return GetProperty(() => NpContractors); }
            private set { SetProperty(() => NpContractors, value); }
        }

        public string NpContractorRef
        {
            get { return GetProperty(() => NpContractorRef); }
            set { SetProperty(() => NpContractorRef, value); }
        }

        private IExcelImportEngine<NovaposhtaBillTtnDto> ExcelImportEngine { get; }

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        public static void BuildMetadata(MetadataBuilder<NovaposhtaBillViewModel> builder)
        {
            builder.Property(x => x.FilePath)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Number)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateInvoice)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NpContractorRef)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            List<NpContractorDto> npContractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(), true);

            NpContractors = npContractors
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            Title = "Анализ счетов НП";
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return Task.CompletedTask;
            }

            IEnumerable<NovaposhtaBillTtnDto> ttns = ImportTtns();

            if (ttns == null)
            {
                return Task.CompletedTask;
            }

            return SendTtnsAsync(ttns);
        }

        private IEnumerable<NovaposhtaBillTtnDto> ImportTtns()
        {
            IEnumerable<NovaposhtaBillTtnDto> resultData = null;

            try
            {
                ExcelImportResult<NovaposhtaBillTtnDto> importResult = ExcelImportEngine.ImportFromXlsx(FilePath);

                if (importResult.IsSuccess)
                {
                    if (importResult.Errors.Any())
                    {
                        ShowValidationResultView("Предупреждения при импорте файла", importResult.Errors);
                    }

                    resultData = importResult.ResultItems;
                }
                else
                {
                    ShowValidationResultView("Ошибки при импорте файла", importResult.Errors);
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {FilePath} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", FilePath);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }

            return resultData;
        }

        private async Task SendTtnsAsync(IEnumerable<NovaposhtaBillTtnDto> ttns)
        {
            try
            {
                byte[] documentData = await FileHelper.ReadBytesAsync(FilePath);

                NovaposhtaBillSaveDto bill = new NovaposhtaBillSaveDto
                {
                    Number = Number,
                    InvoicedOn = DateInvoice.Value,
                    ContractorRef = NpContractorRef,
                    Ttns = ttns.ToList(),
                    Document = documentData
                };

                await WebClient.ExecuteApiRequestAsync(new UpdateNovaposhtaBill(bill));

                MessageFacadeService.ShowNotificationInfo("Данные успешно импортированы");

                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при импорте данных");
                ShowValidationResultView("Ошибки при импорте данных", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to data import");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при импорте данных");
                Logger.LogError(exception, "Error while importing data");
            }
        }

        private void OpenFile()
        {
            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return;
            }

            FilePath = OpenFileDialogService.GetFullFileName();
        }
    }
}