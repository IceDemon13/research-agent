using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceCategoryBudgetViewItem : TelemartViewItemBase
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

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }
        
        public int CategoryPosition
        {
            get { return GetProperty(() => CategoryPosition); }
            set { SetProperty(() => CategoryPosition, value); }
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public int InvoiceBudgetId
        {
            get { return GetProperty(() => InvoiceBudgetId); }
            set { SetProperty(() => InvoiceBudgetId, value); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public static void BuildMetadata(MetadataBuilder<InvoiceCategoryBudgetViewItem> builder)
        {
            builder.Property(x => x.Budget)
                .MatchesRule(x => x is null or > 0 and < 1_000_000_000, () => "Бюджет должен быть в диапазоне от 1 до 1 000 000 000");
        }
    }
}