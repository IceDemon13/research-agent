using System;
using System.Collections.Generic;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.SupplierBill.Create.Parsing
{
    public sealed class SupplierBillTextParserResult
    {
        public SupplierBillTextParserResult(IReadOnlyCollection<CreateSupplierBillProductModel> items)
        {
            IsOk = true;
            Items = items;
            Errors = Array.Empty<ValidationResultItem>();
        }

        public SupplierBillTextParserResult(IReadOnlyCollection<ValidationResultItem> errors)
        {
            IsOk = false;
            Items = Array.Empty<CreateSupplierBillProductModel>();
            Errors = errors;
        }

        public bool IsOk { get; }

        public IReadOnlyCollection<ValidationResultItem> Errors { get; }

        public IReadOnlyCollection<CreateSupplierBillProductModel> Items { get; set; }
    }
}