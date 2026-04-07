using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Telemart.Client.Dictionaries;
using Telemart.Client.Style.Editors;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotCategoryPropertyValueContentViewItem : TelemartViewItemBase
    {
        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public int PropertyId
        {
            get { return GetProperty(() => PropertyId); }
            set { SetProperty(() => PropertyId, value); }
        }

        public RobotPropertyType RobotPropertyType
        {
            get { return GetProperty(() => RobotPropertyType); }
            set { SetProperty(() => RobotPropertyType, value); }
        }

        public string RobotPropertyName => RobotPropertyType?.Name ?? RobotPropertyType.Text.Name;

        public IChangeTracking RobotCategoryValue
        {
            get { return GetProperty(() => RobotCategoryValue); }
            set { SetProperty(() => RobotCategoryValue, value); }
        }

        public IChangeTracking RobotCategoryValueOld
        {
            get { return GetProperty(() => RobotCategoryValueOld); }
            set { SetProperty(() => RobotCategoryValueOld, value); }
        }

        public string PropertyRegex
        {
            get { return GetProperty(() => PropertyRegex); }
            set { SetProperty(() => PropertyRegex, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int GroupPosition
        {
            get { return GetProperty(() => GroupPosition); }
            set { SetProperty(() => GroupPosition, value); }
        }

        public string PropertyDescription
        {
            get { return GetProperty(() => PropertyDescription); }
            set { SetProperty(() => PropertyDescription, value); }
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

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool ActiveNew
        {
            get { return GetProperty(() => ActiveNew); }
            set { SetProperty(() => ActiveNew, value, ActiveValueChanged); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public string ModifiedByName
        {
            get { return GetProperty(() => ModifiedByName); }
            set { SetProperty(() => ModifiedByName, value); }
        }

        public bool IsActiveChanged => Active != ActiveNew;

        private void ActiveValueChanged()
        {
            RaisePropertyChanged(nameof(IsActiveChanged));

            if (RobotCategoryValue is ValidationTextValueWrapper textValueWrapper)
            {
                textValueWrapper.Active = ActiveNew;
            }
            else if (RobotCategoryValue is ValidationMultiComboBoxValue value)
            {
                value.Active = ActiveNew;
            }
            else if (RobotCategoryValue is ValidationComboBoxValue comboBoxValue)
            {
                comboBoxValue.Active = ActiveNew;
            }
        }
    }
}