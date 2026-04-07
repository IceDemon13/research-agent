using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Payments
{
    public class PaymentViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public decimal Fee
        {
            get { return GetProperty(() => Fee); }
            set { SetProperty(() => Fee, value); }
        }

        public decimal ProviderFee
        {
            get { return GetProperty(() => ProviderFee); }
            set { SetProperty(() => ProviderFee, value); }
        }

        public int LimitUah
        {
            get { return GetProperty(() => LimitUah); }
            set { SetProperty(() => LimitUah, value); }
        }

        public int LimitUsd
        {
            get { return GetProperty(() => LimitUsd); }
            set { SetProperty(() => LimitUsd, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
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

        public static void BuildMetadata(MetadataBuilder<PaymentViewItem> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
