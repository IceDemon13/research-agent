using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Core.Serialization.Xml;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.Requests.Features.SupplierBill.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.SupplierBill.Requests;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public sealed class SupplierBillViewModel : TelemartEditorViewModelBase<SupplierBillDto, SupplierBillViewMessage, SupplierBillViewItem>
    {
        private readonly TelegramBotOptions _telegramBotOptions;
        private const int MaxDocumentsCount = 10;
        private const int MaxFileLengthMb = 4;

        private IReadOnlyDictionary<int, string> _supplierNames;
        private IReadOnlyDictionary<int, string> _employeeNames;
        private IReadOnlyDictionary<int, string> _stateNames;

        private bool _canEdit;
        private bool _canEditDocuments;

        public SupplierBillViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            TelegramBotOptions telegramBotOptions)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _telegramBotOptions = telegramBotOptions;
            ProcessBillCommand = new AsyncCommand(ProcessBillAsync);
            CancelBillCommand = new AsyncCommand(CancelBillAsync);
            CancelCompletedCommand = new AsyncCommand(CancelCompletedAsync);
            CompleteBillCommand = new AsyncCommand(CompleteBillAsync);

            RefreshDocumentsCommand = new AsyncCommand(RefreshDocumentsAsync);
            AddDocumentCommand = new DelegateCommand(AddDocument, () => _canEditDocuments);
            RemoveDocumentCommand = new AsyncCommand<SupplierBillDocumentViewItem>(RemoveDocumentAsync, x => x != null && _canEditDocuments);
            PrintDocumentCommand = new AsyncCommand<SupplierBillDocumentViewItem>(PrintDocumentAsync, x => x != null);
            ShowDocumentsBotQrCommand = new DelegateCommand(ShowDocumentsBotQr);

            SerializeIntoXmlCommand = new DelegateCommand(SerializeIntoXml);
            HandleTabSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleTabSelectionChanged);

            Messenger.Register<SupplierBillDocumentMessage>(this, OnSupplierBillDocumentMessage);
        }

        public SupplierBillViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddDocumentCommand { get; }

        public IDelegateCommand ShowDocumentsBotQrCommand { get; }

        public IAsyncCommand RemoveDocumentCommand { get; }

        public IAsyncCommand PrintDocumentCommand { get; }

        public IAsyncCommand RefreshDocumentsCommand { get; }

        public IAsyncCommand ProcessBillCommand { get; }

        public IAsyncCommand CancelBillCommand { get; }

        public IAsyncCommand CancelCompletedCommand { get; }

        public IAsyncCommand CompleteBillCommand { get; }

        public IDelegateCommand SerializeIntoXmlCommand { get; }

        public IDelegateCommand HandleTabSelectionChangedCommand { get; }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 660;

        public override int Width => 800;

        #endregion

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<SupplierBillDocumentType> DocumentTypes
        {
            get { return GetProperty(() => DocumentTypes); }
            private set { SetProperty(() => DocumentTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public DateTime InvoicedOnMinValue
        {
            get { return GetProperty(() => InvoicedOnMinValue); }
            private set { SetProperty(() => InvoicedOnMinValue, value); }
        }

        public DateTime InvoicedOnMaxValue
        {
            get { return GetProperty(() => InvoicedOnMaxValue); }
            private set { SetProperty(() => InvoicedOnMaxValue, value); }
        }

        public bool NotLockedAndNewState => Model?.EmployeeLockId == null && Model?.StateId == SupplierBillState.New.Id;

        public bool NotLockedAndNeedStateForCancel => Model?.EmployeeLockId == null && Model?.StateId == SupplierBillState.Completed.Id;

        public bool NotLockedAndProcessedState => Model?.EmployeeLockId == null && Model?.StateId == SupplierBillState.Processed.Id;

        protected override string CreatedActionMessage => throw new NotSupportedException();

        protected override string EntityName => "Счет поставщика";

        protected override string UpdatedActionMessage => "Обновлен";

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>();

        private IDocumentManagerService SizeableNotMinimizeDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableNotMinimizeDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected override async Task HandleLoadedAsync()
        {
            _stateNames = Dictionaries.GetItems<SupplierBillState>().ToDictionary(x => x.Id, x => x.Name);
            DocumentTypes = Dictionaries.GetItems<SupplierBillDocumentType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshSuppliersAsync(), RefreshEmployeesAsync());

            await base.HandleLoadedAsync();

            SummaryItems = GetSummaryItems();
        }

        protected override bool CanEdit()
        {
            return _canEdit;
        }

        protected override Task<Result<SupplierBillDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(SupplierBillDto dto, MessageType messageType)
        {
            return new SupplierBillMessage(dto, messageType);
        }

        protected override Task<SupplierBillDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QuerySupplierBill(id));
        }

        protected override Task<LockResponse<SupplierBillDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockSupplierBill(id));
        }

        protected override Task<LockResponse<SupplierBillDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockSupplierBill(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            if (Model != null)
            {
                Title = $"Счет поставщика №{Model.Id} от {Model.InvoicedOn:dd.MM.yy}";
            }
        }

        protected override Task<Result<SupplierBillDto>> UpdateEntityAsync()
        {
            SupplierBillSaveDto dto = new SupplierBillSaveDto
            {
                Id = Model.Id,
                Edrpou = Model.Edrpou,
                InvoicedOn = Model.InvoicedOn,
                Number = Model.Number,
                Comment = Model.Comment
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateSupplierBill(Model.Id, dto));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(Model.Documents);
            yield return nameof(Model.Products);
        }

        protected override void AfterSetData()
        {
            _canEdit = Model.StateId == SupplierBillState.New.Id;
            _canEditDocuments = Model.StateId == SupplierBillState.New.Id || Model.StateId == SupplierBillState.Processed.Id;

            InvoicedOnMinValue = Model.CreatedOn.Date.AddMonths(-1);
            InvoicedOnMaxValue = DateTime.Today;

            RaisePropertiesChanged(nameof(NotLockedAndNewState), nameof(NotLockedAndProcessedState), nameof(NotLockedAndNeedStateForCancel));
        }

        protected override void OnInitializeInDesignModeInternal()
        {
            Employees = new[] { new ComboBoxItem(8, "Сазонов Илья") }.ToReadOnlyObservableCollection();
            _employeeNames = Employees.ToDictionary(x => x.Id, y => y.DisplayValue);
            _supplierNames = new Dictionary<int, string> { [1] = "ООО \"Единый Компьютинг\"" };
            _stateNames = new Dictionary<int, string> { [SupplierBillState.New.Id] = SupplierBillState.New.Name };

            Model.Id = 23;
            Model.SupplierId = 1;
            Model.Edrpou = "3773";
            Model.StateId = SupplierBillState.New.Id;
            Model.InvoicedOn = DateTime.Today;
            Model.Comment = "Коментарий";
            Model.CreatedBy = 8;
            Model.CreatedOn = DateTime.Today.AddHours(4).AddMinutes(43);
            Model.ModifiedBy = 8;
            Model.ModifiedOn = DateTime.Now;
            Model.Products = new ObservableCollection<SupplierBillProductViewItem>
            {
                new SupplierBillProductViewItem
                {
                    Id = 1,
                    BillId = 23,
                    ProductId = 2,
                    Name = "Nokia 2310 Black",
                    Quantity = 999,
                    Sum = 1000,
                    SumTax = 1200
                }
            };

            SummaryItems = GetSummaryItems();
        }

        private async Task RefreshSuppliersAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            _supplierNames = contractors.Where(x => x.IsSupplier).ToDictionary(x => x.Id, y => y.Name);
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            _employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);

            Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            const string Format = "C2";

            yield return new SummaryViewItem("Контрагент", _supplierNames.GetValueOrDefault(Model.SupplierId));
            yield return new SummaryViewItem("ЕДРПОУ", Model.Edrpou);

            if (Model.InvoiceId.HasValue)
            {
                yield return new SummaryViewItem("Накладная", Model.InvoiceId.Value.ToString());
            }

            yield return new SummaryViewItem("Статус", _stateNames.GetValueOrDefault(Model.StateId));

            yield return new SummaryViewItem("Валюта", Currency.GetById(Model.CurrencyId).Title);

            if (Model.TotalPrice != null && Model.TotalPriceTax != null)
            {
                yield return new SummaryViewItem("Без НДС", CurrencyFormatingRules.ToStr(Model.TotalPrice.Value, Model.CurrencyId, Format));

                if (Model.Tax)
                {
                    yield return new SummaryViewItem("НДС", CurrencyFormatingRules.ToStr(Model.TotalPriceTax.Value - Model.TotalPrice.Value, Model.CurrencyId, Format));
                    yield return new SummaryViewItem("С НДС", CurrencyFormatingRules.ToStr(Model.TotalPriceTax.Value, Model.CurrencyId, Format));
                }
            }

            yield return new SummaryViewItem("Создал", $"{_employeeNames.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменил", $"{_employeeNames.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
        }

        private void AddDocument()
        {
            if (Model.Documents.Count >= MaxDocumentsCount)
            {
                MessageFacadeService.ShowNotificationWarning($"К счету можно добавить не больше чем {MaxDocumentsCount} файлов");
                return;
            }

            if (OpenFileDialogService.ShowDialog())
            {
                if (OpenFileDialogService.Files.Count() + Model.Documents.Count > MaxDocumentsCount)
                {
                    MessageFacadeService.ShowNotificationWarning($"К счету можно добавить не больше чем {MaxDocumentsCount} файлов");
                    return;
                }

                if (!OpenFileDialogService.Files.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите хотя бы 1 файл");
                    return;
                }

                List<IFileInfo> notValidFiles = OpenFileDialogService.Files.Where(x => x.Length > MaxFileLengthMb.Megabytes().Bytes).ToList();

                if (notValidFiles.Any())
                {
                    ShowValidationResultView(
                        "Ошибки при добавлении файлов",
                        notValidFiles.Select(x => new ValidationResultItem($"Файл \"{x.GetFullName()}\" должен быть меньше {MaxFileLengthMb} MB", true)).ToArray());
                }
                else
                {
                    AddDocumentsParameter parameter = new AddDocumentsParameter(
                        Model.Id,
                        OpenFileDialogService.Files.Select(x => x.GetFullName()).ToList());

                    SupplierBillAddDocumentViewModel viewModel = new SupplierBillAddDocumentViewModel(
                        WebClient,
                        Dictionaries,
                        MessageFacadeService,
                        Messenger);

                    NonModalSizeableDialogDocumentManagerService.ShowView("AddDocumentsView", viewModel, parameter, this);
                }
            }
        }

        private async Task RemoveDocumentAsync(SupplierBillDocumentViewItem document)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteSupplierBillDocument(document.Id));

                Model.Documents.Remove(document);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
                ShowValidationResultView("Ошибки при удалении документа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete supplier bill document");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while removing supplier bill document");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
            }
        }

        private async Task PrintDocumentAsync(SupplierBillDocumentViewItem documentObj)
        {
            SupplierBillDocumentDto document = await WebClient.ExecuteApiRequestAsync(new QuerySupplierBillDocument(documentObj.Id));
            await FileHelper.OpenAsFileAsync(document.Data, document.Ext);
        }

        private async Task RefreshDocumentsAsync()
        {
            try
            {
                List<SupplierBillDocumentDto> documents = await WebClient.ExecuteApiRequestAsync(new QuerySupplierBillDocuments(Model.Id));
                Model.Documents = documents.Select(x => Mapper.Map<SupplierBillDocumentViewItem>(x)).ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get supplier bill documents");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private Task ProcessBillAsync()
        {
            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                Result<SupplierBillDto> result = await WebClient.ExecuteApiRequestAsync(new ProcessSupplierBill(lockedEntity.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Счет обработан с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Счет успешно обработан");
                }
            });
        }

        private Task CancelBillAsync()
        {
            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                Result<SupplierBillDto> result = await WebClient.ExecuteApiRequestAsync(new CancelSupplierBill(lockedEntity.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Счет отменен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Счет успешно отменен");
                }
            });
        }

        private Task CancelCompletedAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите отменить завершение?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                Result<SupplierBillDto> result =
                    await WebClient.ExecuteApiRequestAsync(new CancelCompletedSupplierBill(lockedEntity.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Счет переведен в статус новый с предупреждениями");
                    ShowValidationResultView(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Счет переведен в статус новый");
                }
            });
        }

        private Task CompleteBillAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите завершить счет?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async _ =>
            {
                Result<SupplierBillDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteSupplierBill(Model.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Счет завершен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Счет успешно завершен");
                }
            });
        }

        private void SerializeIntoXml()
        {
            string fileName = $"Supplier_Bill_{Model.Edrpou}_{Model.Number}_{Model.InvoicedOn:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                y =>
                {
                    BillRequest request = new BillRequest(
                        Model.Edrpou,
                        Model.Number,
                        Model.InvoiceId!.Value,
                        Model.InvoiceCarryId!.Value,
                        Currency.GetById(Model.CurrencyId).IsoCodeNumber,
                        Model.InvoicedOn,
                        Model.Products.Select(x => new BillProductRequest(x.ProductId, x.Quantity, Model.Tax ? x.Price : x.PriceTax, x.TaxRate.Value1C, x.Tnved)).ToArray());

                    XmlSerializer<BillRequest> serializer = new XmlSerializer<BillRequest>();

                    File.WriteAllText(SaveFileDialogService.File.GetFullName(), serializer.Serialize(request));
                },
                folderPath,
                fileName);
        }

        private void OnSupplierBillDocumentMessage(SupplierBillDocumentMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Model.Documents.Add(Mapper.Map<SupplierBillDocumentViewItem>(message.Entity));
                    break;
            }
        }

        private void ShowDocumentsBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter(DocumentsBotHelper.GetUrl(Model.Id, Entity.SupplierBillId, Dictionaries, _telegramBotOptions), "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }

        private void HandleTabSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
        }
    }
}