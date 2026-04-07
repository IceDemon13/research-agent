using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseDeliveryViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public ObservableCollection<int> CarryIds
        {
            get { return GetProperty(() => CarryIds); }
            set { SetProperty(() => CarryIds, value); }
        }

        public ObservableCollection<int> EntityTypeIds
        {
            get { return GetProperty(() => EntityTypeIds); }
            set { SetProperty(() => EntityTypeIds, value); }
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

        public string DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            set { SetProperty(() => DaysOfWeek, value); }
        }

        public int? Days
        {
            get { return GetProperty(() => Days); }
            set { SetProperty(() => Days, value); }
        }

        public bool PlanCourierCall
        {
            get { return GetProperty(() => PlanCourierCall); }
            set { SetProperty(() => PlanCourierCall, value); }
        }

        public int? PlannedWeight
        {
            get { return GetProperty(() => PlannedWeight); }
            set { SetProperty(() => PlannedWeight, value); }
        }

        public TimeSpan? TimeGet
        {
            get { return GetProperty(() => TimeGet); }
            set { SetProperty(() => TimeGet, value); }
        }

        public TimeSpan? TimeDeliveryFrom
        {
            get { return GetProperty(() => TimeDeliveryFrom); }
            set { SetProperty(() => TimeDeliveryFrom, value); }
        }

        public TimeSpan? TimeDeliveryTo
        {
            get { return GetProperty(() => TimeDeliveryTo); }
            set { SetProperty(() => TimeDeliveryTo, value); }
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

        public string CarriesString
        {
            get { return GetProperty(() => CarriesString); }
            set { SetProperty(() => CarriesString, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<WarehouseDeliveryViewItem> builder)
        {
            builder.Property(x => x.CarryIds)
                .MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.DaysOfWeek)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Days)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TimeGet)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TimeDeliveryFrom)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TimeDeliveryTo)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PlanCourierCall)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PlannedWeight)
                .MatchesInstanceRule((x, o) => o.PlanCourierCall ? x > 0 : true, () => "Вес должен быть больше 0");

        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public WarehouseDeliveryViewItem Clone()
        {
            WarehouseDeliveryViewItem viewItem = ReflectionObjectCloner.Clone(this);

            viewItem.CarryIds = new ObservableCollection<int>(CarryIds);
            viewItem.EntityTypeIds = new ObservableCollection<int>(EntityTypeIds);

            return viewItem;
        }
    }
}
