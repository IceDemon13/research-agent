using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceAdditionalCostViewItem : TelemartEditorViewItemBase
    {
        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public int? TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? SourceId
        {
            get { return GetProperty(() => SourceId); }
            set { SetProperty(() => SourceId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public decimal? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public int? CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public ObservableCollection<InvoiceAdditionalCostProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public static void BuildMetadata(MetadataBuilder<InvoiceAdditionalCostViewItem> builder)
        {
            builder.Property(x => x.Amount)
                .MatchesRule(x => x > 0 && x <= 100000, () => "Значение должно быть 0 .. 100000");
            builder.Property(x => x.CurrencyId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SourceId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            InvoiceAdditionalCostViewItem item = (InvoiceAdditionalCostViewItem)base.Clone();

            item.Products = Products.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }
    }
}
