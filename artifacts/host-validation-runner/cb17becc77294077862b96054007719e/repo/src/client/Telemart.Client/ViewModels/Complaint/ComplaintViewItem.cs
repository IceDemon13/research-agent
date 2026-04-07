using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintViewItem : TelemartEditorViewItemBase
    {
        public ComplaintState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int SourceId
        {
            get { return GetProperty(() => SourceId); }
            set { SetProperty(() => SourceId, value); }
        }

        public Priority Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public int? TradeInId
        {
            get { return GetProperty(() => TradeInId); }
            set { SetProperty(() => TradeInId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public string Resolution
        {
            get { return GetProperty(() => Resolution); }
            set { SetProperty(() => Resolution, value); }
        }

        public DateTime Deadline
        {
            get { return GetProperty(() => Deadline); }
            set { SetProperty(() => Deadline, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
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

        public static void BuildMetadata(MetadataBuilder<ComplaintViewItem> builder)
        {
            builder.Property(x => x.SourceId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Priority)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Text)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(1000, () => "Значение поля должно быть короче 1000 символов");
            builder.Property(x => x.Deadline)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio)
                .ApplyFioValidationRules(() => "Введите ФИО");
            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email)
                .MaxLength(250, () => "Значение поля должно быть короче 250 символов")
                .MatchesRegularExpression(@"^$|^.+@.+\..+$", () => Resources.OrderViewModel_Email);
        }
    }
}
