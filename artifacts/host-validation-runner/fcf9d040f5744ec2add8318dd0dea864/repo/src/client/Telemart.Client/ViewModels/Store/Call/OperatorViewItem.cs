using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class OperatorViewItem : TelemartViewItemBase
    {
        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public string Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string Status
        {
            get { return GetProperty(() => Status); }
            set { SetProperty(() => Status, value); }
        }

        public string AsteriskStatus
        {
            get { return GetProperty(() => AsteriskStatus); }
            set { SetProperty(() => AsteriskStatus, value); }
        }

        public TimeSpan? MaxBusyTime
        {
            get { return GetProperty(() => MaxBusyTime); }
            set { SetProperty(() => MaxBusyTime, value); }
        }

        public DateTime? LastModifiedOn
        {
            get { return GetProperty(() => LastModifiedOn); }
            set { SetProperty(() => LastModifiedOn, value); }
        }

        public int? LastModifiedBy
        {
            get { return GetProperty(() => LastModifiedBy); }
            set { SetProperty(() => LastModifiedBy, value); }
        }

        public bool AllowChangeMaxBusyTimeByEmployee
        {
            get { return GetProperty(() => AllowChangeMaxBusyTimeByEmployee); }
            set { SetProperty(() => AllowChangeMaxBusyTimeByEmployee, value); }
        }

        public static void BuildMetadata(MetadataBuilder<OperatorViewItem> builder)
        {
            builder.Property(x => x.MaxBusyTime)
                .MatchesInstanceRule(
                    (x, y) => !y.AllowChangeMaxBusyTimeByEmployee || (x.HasValue && x.Value >= TimeSpan.Zero),
                    () => "Максимальное время для статуса \"Занят\" должно быть заполнено");
        }
    }
}