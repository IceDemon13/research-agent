using System;
using System.Collections.Generic;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Invoice.Parsing
{
    public sealed class InvoiceProductTextParserResult
    {
        public InvoiceProductTextParserResult(IReadOnlyCollection<InvoiceProductBulkAddViewItem> items)
        {
            IsOk = true;
            Items = items;
            Errors = Array.Empty<ValidationResultItem>();
        }

        public InvoiceProductTextParserResult(IReadOnlyCollection<ValidationResultItem> errors)
        {
            IsOk = false;
            Items = Array.Empty<InvoiceProductBulkAddViewItem>();
            Errors = errors;
        }

        public bool IsOk { get; }

        public IReadOnlyCollection<ValidationResultItem> Errors { get; }

        public IReadOnlyCollection<InvoiceProductBulkAddViewItem> Items { get; set; }
    }
}