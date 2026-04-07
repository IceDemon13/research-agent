using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.WorkSchedule
{
    public sealed class HolidayViewItem : TelemartCloneableViewItemBase
    {
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

        public TimeSpan? OldStart
        {
            get { return GetProperty(() => OldStart); }
            set { SetProperty(() => OldStart, value); }
        }

        public TimeSpan? Start
        {
            get { return GetProperty(() => Start); }
            set { SetProperty(() => Start, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(End), nameof(IsWeekend), nameof(IsGraphicOverride))); }
        }

        public TimeSpan? OldEnd
        {
            get { return GetProperty(() => OldEnd); }
            set { SetProperty(() => OldEnd, value); }
        }

        public TimeSpan? End
        {
            get { return GetProperty(() => End); }
            set { SetProperty(() => End, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(IsWeekend), nameof(IsGraphicOverride))); }
        }

        public DateTime OldDate
        {
            get { return GetProperty(() => OldDate); }
            set { SetProperty(() => OldDate, value); }
        }

        public DateTime Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }


        public bool IsChanged => OldStart != Start || OldEnd != End || OldDate != Date || Id == 0;

        public bool IsWeekend => Start is null && End is null;

        public bool IsGraphicOverride => Start.HasValue && End.HasValue;

        public string FullName => string.IsNullOrEmpty(ParentTypeName) ? TypeName : $"{ParentTypeName}-{TypeName}";

        public static void BuildMetadata(MetadataBuilder<HolidayViewItem> builder)
        {
            builder.Property(x => x.Date)
                .MatchesRule(x => x.Date != default, () => "Выберите дни");

            builder.Property(x => x.End)
                .MatchesInstanceRule((x, y) => !x.HasValue || x > y.Start, () => "Время окончания должно быть больше времени начала");
        }
    }
}