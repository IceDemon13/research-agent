using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Utils;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class InvoiceTtnViewItemResolver : IValueResolver<InvoiceDto, InvoiceViewItem, ObservableCollection<InvoiceTtnViewItem>>
    {
        public InvoiceTtnViewItemResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public ObservableCollection<InvoiceTtnViewItem> Resolve(
            InvoiceDto source,
            InvoiceViewItem destination,
            ObservableCollection<InvoiceTtnViewItem> destMember,
            ResolutionContext context)
        {
            return source?.InvoiceTtns
                .Select(t => new InvoiceTtnViewItem(Dictionaries.GetItemById<CarryType>(source.CarryId))
                {
                    Id = t.Id,
                    Ttn = t.Ttn
                })
                .ToObservableCollection();
        }
    }
}