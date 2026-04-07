using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceBudgetViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public decimal? Budget
        {
            get { return GetProperty(() => Budget); }
            set { SetProperty(() => Budget, value); }
        }

        public DateTime? DateFrom
        {
            get { return GetProperty(() => DateFrom); }
            set { SetProperty(() => DateFrom, value, () => RaisePropertiesChanged(nameof(CurrentBudget), nameof(DisplayName))); }
        }

        public DateTime? DateTo
        {
            get { return GetProperty(() => DateTo); }
            set { SetProperty(() => DateTo, value, () => RaisePropertiesChanged(nameof(CurrentBudget), nameof(DisplayName))); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int? ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public bool CurrentBudget => DateFrom != null && DateTo != null && DateFrom.Value.Date <= DateTime.Now.Date && DateTo.Value.Date >= DateTime.Now.Date;

        public string DisplayName => DateFrom.HasValue && DateTo.HasValue ? $"{DateFrom.Value:dd.MM.yyyy} - {DateTo.Value:dd.MM.yyyy} ({Budget}$)" : string.Empty;

        public static void BuildMetadata(MetadataBuilder<InvoiceBudgetViewItem> builder)
        {
            builder.Property(x => x.Budget)
                .MatchesRule(x => x is > 0 and < 1_000_000_000, () => "Бюджет должен быть в диапазоне от 1 до 1 000 000 000");

            builder.Property(x => x.DateFrom)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.DateTo)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}