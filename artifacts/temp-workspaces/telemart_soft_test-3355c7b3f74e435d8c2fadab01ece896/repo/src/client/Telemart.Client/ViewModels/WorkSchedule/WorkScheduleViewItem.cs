using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.WorkSchedule
{
    public class WorkScheduleViewItem : TelemartCloneableViewItemBase
    {
        public WorkScheduleViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public string ParentTypeName
        {
            get { return GetProperty(() => ParentTypeName); }
            set { SetProperty(() => ParentTypeName, value, () => RaisePropertyChanged(nameof(FullName))); }
        }

        public string TypeName
        {
            get { return GetProperty(() => TypeName); }
            set { SetProperty(() => TypeName, value, () => RaisePropertyChanged(nameof(FullName))); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string OldName
        {
            get { return GetProperty(() => OldName); }
            set { SetProperty(() => OldName, value); }
        }

        public TimeSpan? OldStart
        {
            get { return GetProperty(() => OldStart); }
            set { SetProperty(() => OldStart, value); }
        }

        public TimeSpan? Start
        {
            get { return GetProperty(() => Start); }
            set { SetProperty(() => Start, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(End), nameof(Time))); }
        }

        public TimeSpan? OldEnd
        {
            get { return GetProperty(() => OldEnd); }
            set { SetProperty(() => OldEnd, value); }
        }

        public TimeSpan? End
        {
            get { return GetProperty(() => End); }
            set { SetProperty(() => End, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(Time))); }
        }

        public string OldDays
        {
            get { return GetProperty(() => OldDays); }
            set { SetProperty(() => OldDays, value); }
        }

        public string Days
        {
            get { return GetProperty(() => Days); }
            set { SetProperty(() => Days, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public string Time => $"({Start?.ToString(@"hh\:mm")}-{End?.ToString(@"hh\:mm")})";

        public string FullName => string.IsNullOrEmpty(ParentTypeName) ? TypeName : $"{ParentTypeName}-{TypeName}";

        public bool IsChanged => OldStart != Start || OldEnd != End || OldDays != Days || OldName != Name || Id == 0;

        public static void BuildMetadata(MetadataBuilder<WorkScheduleViewItem> builder)
        {
            builder.Property(x => x.Days)
                .MatchesRule(x => !string.IsNullOrEmpty(x), () => "Выберите дни");

            builder.Property(x => x.End)
                .MatchesInstanceRule((x, y) => x.HasValue && x > y.Start, () => "Время окончания должно быть больше времени Начало");

            builder.Property(x => x.Start)
                .MatchesRule(x => x.HasValue, () => "Обязательно к заполнению");
        }
    }
}