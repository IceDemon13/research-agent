using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class ProductScanSerialsViewModel : TelemartDialogViewModelBase
    {
        private ProductAttributesDto productAttributesDto;
        private HashSet<string> accountingSystemSerials;
        private HashSet<string> existingSerials;
        private HashSet<string> barcodes;

        private IReadOnlyCollection<ProductSnLengthDto> initialSerialNumberLength;
        private IReadOnlyCollection<string> validSerialNumbers;
        private string errorForNotValidSerialNumbers;
        private string existingSerialsError;

        private int? firstSerialNumberLength;

        private bool ignoreLengthValidation;
        private bool scanRangeInProgress;
        private bool scanExistingSerials;
        private int minSerialCount;
        private int? maxSerialCount;

        public ProductScanSerialsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderRules orderRules)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderRules = orderRules;

            DeleteSerialNumberCommand = new DelegateCommand<string>(DeleteSerialNumber, x => !string.IsNullOrEmpty(x));
            AddSerialNumberCommand = new DelegateCommand<string>(AddSerialNumber, x => !string.IsNullOrEmpty(x));
            WindowClosingCommand = new DelegateCommand<CancelEventArgs>(WindowClosing);

            CanScanning = true;
            CanChangeScanMode = true;
            SerialNumbers = new ObservableCollection<string>();
        }

        public ProductScanSerialsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddSerialNumberCommand { get; }

        public IDelegateCommand DeleteSerialNumberCommand { get; }

        public IDelegateCommand WindowClosingCommand { get; }

        #endregion

        #region INPC

        public string SelectedSerialNumber
        {
            get { return GetProperty(() => SelectedSerialNumber); }
            set { SetProperty(() => SelectedSerialNumber, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public ObservableCollection<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            private set { SetProperty(() => SerialNumbers, value); }
        }

        public ScanSerialMode ScanMode
        {
            get { return GetProperty(() => ScanMode); }
            set { SetProperty(() => ScanMode, value); }
        }

        public bool CanChangeScanMode
        {
            get { return GetProperty(() => CanChangeScanMode); }
            private set { SetProperty(() => CanChangeScanMode, value); }
        }

        public bool CanScanning
        {
            get { return GetProperty(() => CanScanning); }
            private set { SetProperty(() => CanScanning, value); }
        }

        #endregion

        private IOrderRules OrderRules { get; }

        protected override async Task HandleLoadedAsync()
        {
            ProductSerialsViewModelParameter p = (ProductSerialsViewModelParameter)Parameter;

            existingSerials = new HashSet<string>(p.ExistingSerials);
            barcodes = new HashSet<string>(p.Barcodes, StringComparer.OrdinalIgnoreCase);

            initialSerialNumberLength = p.SerialNumberLength;
            scanExistingSerials = p.ScanExistingSerials;
            existingSerialsError = p.ExistingSerialsError;
            ignoreLengthValidation = p.IgnoreLengthValidation;
            errorForNotValidSerialNumbers = p.ErrorForNotValidSerialNumbers;
            validSerialNumbers = p.ValidSerialNumbers;

            if (p.AccountingSystemSerials != null)
            {
                accountingSystemSerials = new HashSet<string>(p.AccountingSystemSerials, StringComparer.OrdinalIgnoreCase);
            }

            ScanMode = p.Mode;
            minSerialCount = p.MinSerialCount;
            maxSerialCount = p.MaxSerialCount;

            Title = p.Title;

            productAttributesDto = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributes(p.ProductId));
        }

        protected override Task HandleOkAsync()
        {
            if (!string.IsNullOrWhiteSpace(SerialNumber))
            {
                MessageFacadeService.ShowNotificationWarning("Поле для ввода SN не пустое");
                return Task.CompletedTask;
            }

            if (scanRangeInProgress)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала завершите сканирование диапазона");
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private void AddSerialNumber(string serialNumber)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                return;
            }

            if (SerialNumbers.Count >= maxSerialCount)
            {
                MessageFacadeService.ShowNotificationWarning("Достигнуто максимальное количество серийных номеров", true);
                return;
            }

            if (serialNumber == productAttributesDto.PartNumber)
            {
                System.Media.SystemSounds.Hand.Play();

                if (!MessageFacadeService.Confirm("Вы уверены, что просканировали SN, а не артикул?"))
                {
                    SerialNumber = null;
                    return;
                }
            }

            SerialNumber = null;

            if (!CanScanning && !serialNumber.Equals(BarcodeConstants.CmdFinish, StringComparison.Ordinal))
            {
                MessageFacadeService.ShowNotificationWarning("Сканирование диапазона завершено");
                return;
            }

            if (serialNumber.Equals(BarcodeConstants.CmdFinish, StringComparison.Ordinal))
            {
                OkCommand.Execute(null);
                return;
            }

            string errorMessage = ValidateSerialNumber(serialNumber);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageFacadeService.ShowNotificationError(errorMessage, true);
                return;
            }

            if (!scanRangeInProgress)
            {
                AddSerialNumberToSerials(serialNumber);

                CanChangeScanMode = false;

                switch (ScanMode)
                {
                    case ScanSerialMode.Single:
                        OkCommand.Execute(null);
                        break;
                    case ScanSerialMode.Range:
                        scanRangeInProgress = true;
                        break;
                }
            }
            else
            {
                string firstSerialInRange = SerialNumbers.Last();

                if (firstSerialInRange.Length != serialNumber.Length)
                {
                    MessageFacadeService.ShowNotificationError("Длина первого и последнего SN из диапазона не совпадает");
                    return;
                }

                SerialNumbersRange serialNumbersRange = new SerialNumbersRange(firstSerialInRange, serialNumber);

                if (serialNumbersRange.Range.Length == 0)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при генерации диапазона SN");
                    return;
                }

                string[] serials = serialNumbersRange.Range.Skip(1).ToArray();

                string firstError = serials
                    .Select(ValidateSerialNumber)
                    .FirstOrDefault(x => x != null);

                if (string.IsNullOrWhiteSpace(firstError))
                {
                    scanRangeInProgress = false;
                    CanScanning = false;
                    AddSerialNumbersToSerials(serials);
                }
                else
                {
                    MessageFacadeService.ShowNotificationError(firstError, true);
                }
            }
        }

        private void AddSerialNumbersToSerials(IEnumerable<string> serialNumbers)
        {
            foreach (string serialNumber in serialNumbers)
            {
                AddSerialNumberToSerials(serialNumber);
            }
        }

        private void AddSerialNumberToSerials(string serialNumber)
        {
            if (accountingSystemSerials != null && !accountingSystemSerials.Contains(serialNumber))
            {
                MessageFacadeService.ShowNotificationWarning($"SN {serialNumber} не найден в 1С", true);
            }

            if (firstSerialNumberLength == null)
            {
                firstSerialNumberLength = serialNumber.Length;
            }

            SerialNumbers.Add(serialNumber);
        }

        private void DeleteSerialNumber(string sn)
        {
            if (SerialNumbers.Count == minSerialCount)
            {
                MessageFacadeService.ShowNotificationWarning("Достигнуто минимальное количество серийных номеров");
                return;
            }

            int index = SerialNumbers.IndexOf(sn);
            SelectedSerialNumber = SerialNumbers.RemoveAtAndGetNext(index);

            if (scanRangeInProgress)
            {
                scanRangeInProgress = false;
            }

            CanChangeScanMode = !SerialNumbers.Any();
        }

        private string ValidateSerialNumber(string sn)
        {
            if (existingSerials.Contains(sn) && !scanExistingSerials)
            {
                return "Такой SN уже был обработан";
            }

            if (!existingSerials.Contains(sn) && scanExistingSerials)
            {
                return existingSerialsError;
            }

            if (SerialNumbers.Contains(sn))
            {
                return "Такой SN уже есть в списке";
            }

            if (OrderRules.IsOurBarcode(sn) || barcodes.Contains(sn))
            {
                return "Это ШК, а не SN";
            }

            if (validSerialNumbers?.Any() == true && !validSerialNumbers.Contains(sn))
            {
                return $"{errorForNotValidSerialNumbers} ({sn})";
            }

            if (initialSerialNumberLength?.Any() == true)
            {
                return OrderRules.ValidateSerialNumber(sn, initialSerialNumberLength.ToList(), ignoreLengthValidation);
            }

            List<ProductSnLengthDto> firstSerialNumberLengthDto = firstSerialNumberLength.HasValue ? new[] { new ProductSnLengthDto() { Length = firstSerialNumberLength.Value } }.ToList() : null;

            return OrderRules.ValidateSerialNumber(sn, firstSerialNumberLengthDto, ignoreLengthValidation);
        }

        private void WindowClosing(CancelEventArgs e)
        {
            e.Cancel = !IsOk && SerialNumbers.Any() && !MessageFacadeService.Confirm("Закрыть диалог?");
        }
    }
}