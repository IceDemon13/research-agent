using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class RobotCategoryPropertyValueSaveViewItem : TelemartEditorViewItemBase
    {
        public RobotCategoryPropertyValueSaveViewItem(int id, int propertyId, string propertyDisplayName, string propertyGroupName, string categoryPropertyValue, string categoryPropertyValueNew, bool active, bool activeNew, bool isChanged, int valueSaveTypeId)
        {
            Id = id;
            PropertyId = propertyId;
            PropertyDisplayName = propertyDisplayName;
            PropertyGroupName = propertyGroupName;
            CategoryPropertyValue = categoryPropertyValue;
            CategoryPropertyValueNew = categoryPropertyValueNew;
            Active = active;
            ActiveNew = activeNew;
            IsChanged = isChanged;
            ValueSaveTypeId = valueSaveTypeId;
        }

        public RobotCategoryPropertyValueSaveViewItem()
        {
        }

        public int PropertyId
        {
            get { return GetProperty(() => PropertyId); }
            set { SetProperty(() => PropertyId, value); }
        }

        public string PropertyDisplayName
        {
            get { return GetProperty(() => PropertyDisplayName); }
            set { SetProperty(() => PropertyDisplayName, value); }
        }

        public string PropertyGroupName
        {
            get { return GetProperty(() => PropertyGroupName); }
            set { SetProperty(() => PropertyGroupName, value); }
        }

        public string CategoryPropertyValue
        {
            get { return GetProperty(() => CategoryPropertyValue); }
            set { SetProperty(() => CategoryPropertyValue, value); }
        }

        public string CategoryPropertyValueDisplay
        {
            get { return GetProperty(() => CategoryPropertyValueDisplay); }
            set { SetProperty(() => CategoryPropertyValueDisplay, value); }
        }

        public string CategoryPropertyValueNew
        {
            get { return GetProperty(() => CategoryPropertyValueNew); }
            set { SetProperty(() => CategoryPropertyValueNew, value); }
        }

        public string CategoryPropertyValueDisplayNew
        {
            get { return GetProperty(() => CategoryPropertyValueDisplayNew); }
            set { SetProperty(() => CategoryPropertyValueDisplayNew, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool ActiveNew
        {
            get { return GetProperty(() => ActiveNew); }
            set { SetProperty(() => ActiveNew, value); }
        }

        public int ValueSaveTypeId
        {
            get { return GetProperty(() => ValueSaveTypeId); }
            set { SetProperty(() => ValueSaveTypeId, value); }
        }

        public bool IsChanged
        {
            get { return GetProperty(() => IsChanged); }
            set { SetProperty(() => IsChanged, value); }
        }

        public static void BuildMetadata(MetadataBuilder<RobotCategoryPropertyValueSaveViewItem> builder)
        {
            builder.Property(x => x.ValueSaveTypeId)
                .MatchesRule(
                    (x) => x > 0,
                    () => "Выберите режим сохранения");
        }
    }
}