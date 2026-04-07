using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value, TypeChanged); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string House
        {
            get { return GetProperty(() => House); }
            set { SetProperty(() => House, value); }
        }

        public string NpStreetRef
        {
            get { return GetProperty(() => NpStreetRef); }
            set { SetProperty(() => NpStreetRef, value); }
        }

        public string NpWarehouseRef
        {
            get { return GetProperty(() => NpWarehouseRef); }
            set { SetProperty(() => NpWarehouseRef, value); }
        }

        public string TagsHeader
        {
            get { return GetProperty(() => TagsHeader); }
            set { SetProperty(() => TagsHeader, value); }
        }

        public int? LocationId
        {
            get { return GetProperty(() => LocationId); }
            set { SetProperty(() => LocationId, value); }
        }

        public int CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int MaxPackageWeight
        {
            get { return GetProperty(() => MaxPackageWeight); }
            set { SetProperty(() => MaxPackageWeight, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int? BufferWarehouseId
        {
            get { return GetProperty(() => BufferWarehouseId); }
            set { SetProperty(() => BufferWarehouseId, value); }
        }

        public int? AssemblyWarehouseId
        {
            get { return GetProperty(() => AssemblyWarehouseId); }
            set { SetProperty(() => AssemblyWarehouseId, value); }
        }

        public int? EmployeeAssemblyId
        {
            get { return GetProperty(() => EmployeeAssemblyId); }
            set { SetProperty(() => EmployeeAssemblyId, value); }
        }

        public int? EmployeeAdditionalServiceId
        {
            get { return GetProperty(() => EmployeeAdditionalServiceId); }
            set { SetProperty(() => EmployeeAdditionalServiceId, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public ComboBoxItem? Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value, () => EmployeeId = Employee?.Id ?? 0); }
        }

        public ComboBoxItem? EmployeeAssembly
        {
            get { return GetProperty(() => EmployeeAssembly); }
            set { SetProperty(() => EmployeeAssembly, value, () => EmployeeAssemblyId = EmployeeAssembly?.Id); }
        }

        public ComboBoxItem? EmployeeAdditionalService
        {
            get { return GetProperty(() => EmployeeAdditionalService); }
            set { SetProperty(() => EmployeeAdditionalService, value, () => EmployeeAdditionalServiceId = EmployeeAdditionalService?.Id); }
        }

        public ComboBoxItem? BufferWarehouse
        {
            get { return GetProperty(() => BufferWarehouse); }
            set { SetProperty(() => BufferWarehouse, value, () => BufferWarehouseId = BufferWarehouse?.Id); }
        }

        public ComboBoxItem? AssemblyWarehouse
        {
            get { return GetProperty(() => AssemblyWarehouse); }
            set { SetProperty(() => AssemblyWarehouse, value, () => AssemblyWarehouseId = AssemblyWarehouse?.Id == 0 ? null : AssemblyWarehouse?.Id); }
        }

        public int Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => { RaisePropertiesChanged(nameof(IsActive), nameof(Employee)); }); }
        }

        public bool IsActive
        {
            get => Active == 1;
            set => Active = value ? 1 : 0;
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

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string Longitude
        {
            get { return GetProperty(() => Longitude); }
            set { SetProperty(() => Longitude, value); }
        }

        public string Latitude
        {
            get { return GetProperty(() => Latitude); }
            set { SetProperty(() => Latitude, value); }
        }

        public bool UseCells
        {
            get { return GetProperty(() => UseCells); }
            set { SetProperty(() => UseCells, value); }
        }

        public bool AutoSource
        {
            get { return GetProperty(() => AutoSource); }
            set { SetProperty(() => AutoSource, value); }
        }

        public TimeSpan? Quota
        {
            get { return GetProperty(() => Quota); }
            set { SetProperty(() => Quota, value); }
        }

        public int? PerformancePatternId
        {
            get { return GetProperty(() => PerformancePatternId); }
            set { SetProperty(() => PerformancePatternId, value, () => RaisePropertiesChanged(nameof(Quota), nameof(LocationId))); }
        }

        public bool IsNotReadonlyBufferWarehouse => TypeId == WarehouseKind.Assembly.Id;

        public bool IsNotReadonlyAssemblyWarehouse => TypeId == WarehouseKind.Pickup.Id || TypeId == WarehouseKind.Main.Id;

        public bool IsNotReadonlyEmployeeAssembly => TypeId == WarehouseKind.Assembly.Id;

        public bool AllowCells => TypeId == WarehouseKind.Pickup.Id;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<WarehouseViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Employee)
                .RequiredActive(x => x.IsActive);
            builder.Property(x => x.EmployeeAssembly)
                .MatchesInstanceRule((x, y) => (x.HasValue && x.Value.Active) || !y.IsNotReadonlyEmployeeAssembly, () => "Поле не заполнено либо заполнено неактивным значением");
            builder.Property(x => x.EmployeeAdditionalService)
                .RequiredActive(x => x.IsActive);
            builder.Property(x => x.BufferWarehouseId)
               .MatchesInstanceRule((x, y) => !y.IsNotReadonlyBufferWarehouse || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.AssemblyWarehouseId)
              .MatchesInstanceRule((x, y) => !y.IsNotReadonlyAssemblyWarehouse || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Quota)
                .MatchesInstanceRule((x, y) => y.PerformancePatternId != (int)WarehousePerformancePattern.Quota || x.HasValue, () => Resources.RequiredErrorMessage)
                .MatchesRule(x => x == null || (x >= TimeSpan.Zero && x <= new TimeSpan(31, 0, 0, 0)), () => "Значение должно быть в пределах 0...31 день");
            builder.Property(x => x.LocationId)
                .MatchesInstanceRule((x, y) => y.PerformancePatternId != (int)WarehousePerformancePattern.Quota || x.HasValue, () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public WarehouseViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        private void TypeChanged()
        {
            if (TypeId != WarehouseKind.Pickup.Id && TypeId != WarehouseKind.Main.Id)
            {
                AssemblyWarehouse = null;
            }

            RaisePropertiesChanged(
                nameof(EmployeeAssemblyId),
                nameof(IsNotReadonlyEmployeeAssembly),
                nameof(BufferWarehouseId),
                nameof(IsNotReadonlyBufferWarehouse),
                nameof(AssemblyWarehouseId),
                nameof(IsNotReadonlyAssemblyWarehouse),
                nameof(AllowCells));

            UseCells &= AllowCells;
        }
    }
}