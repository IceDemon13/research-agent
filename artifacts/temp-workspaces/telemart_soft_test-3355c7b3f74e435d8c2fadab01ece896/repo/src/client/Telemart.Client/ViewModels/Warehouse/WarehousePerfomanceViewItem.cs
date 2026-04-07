using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehousePerfomanceViewItem : TelemartEditorViewItemBase
    {
        public string DayOfWeek
        {
            get { return GetProperty(() => DayOfWeek); }
            set { SetProperty(() => DayOfWeek, value); }
        }

        public int WorkId
        {
            get { return GetProperty(() => WorkId); }
            set { SetProperty(() => WorkId, value, () => RaisePropertiesChanged(nameof(AdditionalServiceId), nameof(Estimate), nameof(Performance), nameof(SubdivisionId))); }
        }

        public bool Activity
        {
            get { return GetProperty(() => Activity); }
            set { SetProperty(() => Activity, value); }
        }

        public TimeSpan? Estimate
        {
            get { return GetProperty(() => Estimate); }
            set { SetProperty(() => Estimate, value); }
        }

        public int Performance
        {
            get { return GetProperty(() => Performance); }
            set { SetProperty(() => Performance, value); }
        }

        public int? AdditionalServiceId
        {
            get { return GetProperty(() => AdditionalServiceId); }
            set { SetProperty(() => AdditionalServiceId, value); }
        }

        public string AdditionalServiceName
        {
            get { return GetProperty(() => AdditionalServiceName); }
            set { SetProperty(() => AdditionalServiceName, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int? SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            set { SetProperty(() => SubdivisionId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<WarehousePerfomanceViewItem> builder)
        {
            builder.Property(x => x.DayOfWeek)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WorkId)
               .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Performance)
                .MatchesInstanceRule((x, y) => y.WorkId == WarehouseWorkTypeIds.AdditionalServiceWorkTypeId || x != 0, () => "Значение не может быть 0");
            builder.Property(x => x.AdditionalServiceId)
                .MatchesInstanceRule((x, y) => y.WorkId != WarehouseWorkTypeIds.AdditionalServiceWorkTypeId || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Estimate)
                .MatchesInstanceRule((x, y) => y.WorkId != WarehouseWorkTypeIds.AdditionalServiceWorkTypeId || x.HasValue, () => Resources.RequiredErrorMessage)
                .MatchesRule(x => !x.HasValue || x.Value.Days < 20, () => "Значение должно быть меньше 20 дней")
                .MatchesRule(x => !x.HasValue || x.Value.Days >= 0, () => "Значение должно быть положительным");
            builder.Property(x => x.SubdivisionId)
                .MatchesInstanceRule((x, y) => y.WorkId == WarehouseWorkTypeIds.AssemblyServiceWorkTypeId || (x ?? 0) == 0, () => "Не может быть заполнено если тип работы не Сборка");
        }
    }
}
