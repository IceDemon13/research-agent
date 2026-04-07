using System;
using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class ProductSerialsViewModelParameter
    {
        public ProductSerialsViewModelParameter(
             int productId,
             IReadOnlyCollection<string> existingSerials,
             IReadOnlyCollection<string> barcodes,
             IReadOnlyCollection<ProductSnLengthDto> serialNumberLength,
             ScanSerialMode mode,
             IReadOnlyCollection<string> accountingSystemSerials = null,
             int minSerialCount = 0,
             bool scanExistingSerials = false,
             IReadOnlyCollection<string> validSerialNumbers = null,
             string errorForNotValidSerialNumbers = null,
             int? maxSerialCount = null,
             string title = "Редактирование SN",
             string existingSerialsError = "SN не соответствует товару,\nна который оказана услуга",
             bool ignoreLengthValidation = false)
        {
            ExistingSerials = existingSerials ?? throw new ArgumentNullException(nameof(existingSerials));
            Barcodes = barcodes ?? throw new ArgumentNullException(nameof(barcodes));
            SerialNumberLength = serialNumberLength;
            Mode = mode;
            MaxSerialCount = maxSerialCount;
            AccountingSystemSerials = accountingSystemSerials;
            MinSerialCount = minSerialCount;
            ScanExistingSerials = scanExistingSerials;
            ValidSerialNumbers = validSerialNumbers;
            ErrorForNotValidSerialNumbers = errorForNotValidSerialNumbers;
            ExistingSerialsError = existingSerialsError;
            Title = title;
            IgnoreLengthValidation = ignoreLengthValidation;
            ProductId = productId;
        }

        public int ProductId { get; }

        public IReadOnlyCollection<string> Barcodes { get; }

        public IReadOnlyCollection<string> AccountingSystemSerials { get; }

        public IReadOnlyCollection<string> ExistingSerials { get; }

        public IReadOnlyCollection<ProductSnLengthDto> SerialNumberLength { get; }

        public IReadOnlyCollection<string> ValidSerialNumbers { get; }

        public string ErrorForNotValidSerialNumbers { get; }

        public string ExistingSerialsError { get; }

        public string Title { get; }

        public ScanSerialMode Mode { get; }

        public int MinSerialCount { get; }

        public bool ScanExistingSerials { get; }

        public int? MaxSerialCount { get; }

        public bool IgnoreLengthValidation { get; }
    }
}