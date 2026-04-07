using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public sealed class RefundViewItem : BindableBase, ICloneable, IDataErrorInfo, ILockableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int OrderClientId
        {
            get { return GetProperty(() => OrderClientId); }
            set { SetProperty(() => OrderClientId, value); }
        }

        public int OrderSubdivisionId
        {
            get { return GetProperty(() => OrderSubdivisionId); }
            set { SetProperty(() => OrderSubdivisionId, value); }
        }

        public int? ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public Payment Payment
        {
            get { return GetProperty(() => Payment); }
            set { SetProperty(() => Payment, value, () => RaisePropertiesChanged(nameof(CashboxId), nameof(CashboxNotValid))); }
        }

        public LegalEntityDto LegalEntity
        {
            get { return GetProperty(() => LegalEntity); }
            set { SetProperty(() => LegalEntity, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Inn
        {
            get { return GetProperty(() => Inn); }
            set { SetProperty(() => Inn, value); }
        }

        public string Iban
        {
            get { return GetProperty(() => Iban); }
            set { SetProperty(() => Iban, value); }
        }

        public string CardNumber
        {
            get { return GetProperty(() => CardNumber); }
            set { SetProperty(() => CardNumber, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int? OrderPaymentId
        {
            get { return GetProperty(() => OrderPaymentId); }
            set { SetProperty(() => OrderPaymentId, value); }
        }

        public Currency Currency
        {
            get
            {
                return GetProperty(() => Currency);
            }

            set
            {
                SetProperty(() => Currency, value, () =>
                {
                    if (Currency != null)
                    {
                        CurrencyId = Currency.Id;
                    }
                });
            }
        }

        public int? CashboxId
        {
            get { return GetProperty(() => CashboxId); }
            set { SetProperty(() => CashboxId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value, () => { RaisePropertyChanged(nameof(IsCompleted)); }); }
        }

        public int OrderStateId
        {
            get { return GetProperty(() => OrderStateId); }
            set { SetProperty(() => OrderStateId, value); }
        }

        public bool CompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => CompletedOnFiscalRegistrar); }
            set { SetProperty(() => CompletedOnFiscalRegistrar, value); }
        }

        public string FiscalId
        {
            get { return GetProperty(() => FiscalId); }
            set { SetProperty(() => FiscalId, value); }
        }

        public bool RealCompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => RealCompletedOnFiscalRegistrar); }
            set { SetProperty(() => RealCompletedOnFiscalRegistrar, value); }
        }

        public RefundState State
        {
            get
            {
                return GetProperty(() => State);
            }

            set
            {
                SetProperty(() => State, value, () =>
                {
                    if (State != null)
                    {
                        StateId = State.Id;
                    }
                });
            }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
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

        public DateTime? ApprovedOn
        {
            get { return GetProperty(() => ApprovedOn); }
            set { SetProperty(() => ApprovedOn, value); }
        }

        public int? ApprovedBy
        {
            get { return GetProperty(() => ApprovedBy); }
            set { SetProperty(() => ApprovedBy, value); }
        }

        public DateTime? PayedOn
        {
            get { return GetProperty(() => PayedOn); }
            set { SetProperty(() => PayedOn, value); }
        }

        public int? PayedBy
        {
            get { return GetProperty(() => PayedBy); }
            set { SetProperty(() => PayedBy, value); }
        }

        public bool CashboxNotValid
        {
            get { return GetProperty(() => CashboxNotValid); }
            set { SetProperty(() => CashboxNotValid, value); }
        }

        public bool IsCompleted => StateId == RefundState.Done.Id || StateId == RefundState.Canceled.Id;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<RefundViewItem> builder)
        {
            builder.Property(x => x.CashboxId).MatchesInstanceRule((_, y) => y.Payment?.Id != Payment.CashId || y.CashboxId.HasValue || y.OrderStateId == OrderStatus.DidNotTake.Id || y.OrderStateId == 0, () => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => (y.Payment?.Id != Payment.CashId && !x.HasValue) || !y.CashboxNotValid, () => "Касса не соответсвует выбранному способу");
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Description)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(255);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public RefundViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }
    }
}