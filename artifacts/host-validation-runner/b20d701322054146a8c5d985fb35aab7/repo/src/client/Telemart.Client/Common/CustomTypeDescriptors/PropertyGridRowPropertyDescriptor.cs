using System;
using System.ComponentModel;

namespace Telemart.Client.Common.CustomTypeDescriptors
{
    public class PropertyGridRowPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyGridRow _row;

        public PropertyGridRowPropertyDescriptor(PropertyGridRow propertyGridRow)
            : base(propertyGridRow.Name, new Attribute[] { new BrowsableAttribute(true) })
        {
            _row = propertyGridRow;
        }

        public override bool CanResetValue(object component) => false;

        public override object? GetValue(object? component) => _row.Value;

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
            _row.Value = value;
        }

        public override bool ShouldSerializeValue(object component) => false;

        public override Type ComponentType => typeof(PropertyGridRow);

        public override bool IsReadOnly => _row.IsReadOnly;

        public override Type PropertyType => _row.Type;

        public override string DisplayName => _row.DisplayName;

        public override string Category => _row.GroupName;
    }
}