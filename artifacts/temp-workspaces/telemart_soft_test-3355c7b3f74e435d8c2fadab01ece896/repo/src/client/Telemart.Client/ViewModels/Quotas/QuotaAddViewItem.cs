using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Quotas
{
    public class QuotaAddViewItem : TelemartViewItemBase
    {
        public QuotaAddViewItem(int departmentId, int? oldValue, int planValue)
        {
            DepartmentId = departmentId;
            OldValue = oldValue;
            PlanValue = planValue;
        }

        public int DepartmentId
        {
            get { return GetProperty(() => DepartmentId); }
            set { SetProperty(() => DepartmentId, value); }
        }

        public int? OldValue
        {
            get { return GetProperty(() => OldValue); }
            set { SetProperty(() => OldValue, value); }
        }

        public int NewValue
        {
            get { return GetProperty(() => NewValue); }
            set { SetProperty(() => NewValue, value); }
        }

        public int CalculatedNewValue
        {
            get { return GetProperty(() => CalculatedNewValue); }
            set { SetProperty(() => CalculatedNewValue, value); }
        }

        public int PlanValue
        {
            get { return GetProperty(() => PlanValue); }
            set { SetProperty(() => PlanValue, value); }
        }

        public int Duty
        {
            get { return GetProperty(() => Duty); }
            set { SetProperty(() => Duty, value, OnDutyChanged); }
        }

        public static void BuildMetadata(MetadataBuilder<QuotaAddViewItem> builder)
        {
            builder.Property(x => x.NewValue)
                .MatchesRule(x => x >= 0 && x < 1000, () => "Значение должно быть в пределах 0...999")
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PlanValue)
                .MatchesRule(x => x >= 0 && x < 1000, () => "Значение должно быть в пределах 0...999")
                .Required(() => Resources.RequiredErrorMessage);
        }

        private void OnDutyChanged()
        {
            CalculatedNewValue = PlanValue - Duty;
            NewValue = CalculatedNewValue;
        }
    }
}