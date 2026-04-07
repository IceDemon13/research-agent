using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class OrganizationAccountViewItem : ICloneable
    {
        protected OrganizationAccountViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int OrganizationId { get; set; }

        public virtual int BankId { get; set; }

        public virtual BankDto Bank { get; set; }

        public virtual int CurrencyId { get; set; }

        public virtual Currency Currency { get; set; }

        public virtual int CashboxId { get; set; }

        public virtual bool IsDefault { get; set; }

        public virtual string Name { get; set; }

        public virtual string Account { get; set; }

        public virtual bool Active { get; set; }

        public virtual ObservableCollection<Payment> Payments { get; set; }

        public static OrganizationAccountViewItem Create()
        {
            OrganizationAccountViewItem viewItem = ViewModelSource<OrganizationAccountViewItem>.Create();

            viewItem.Active = true;

            return viewItem;
        }

        public static void BuildMetadata(MetadataBuilder<OrganizationAccountViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(OrganizationConstants.AccountNameMaxLength);

            builder.Property(x => x.Account)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(OrganizationConstants.AccountMaxLength);

            builder.Property(x => x.Bank)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Currency)
                .Required(() => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public OrganizationAccountViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this, Create);
        }

        protected void OnBankChanged(BankDto oldBank)
        {
            if (Bank != null)
            {
                BankId = Bank.Id;
            }
        }

        protected void OnCurrencyChanged(Currency oldCurrency)
        {
            if (Currency != null)
            {
                CurrencyId = Currency.Id;
            }
        }
    }
}