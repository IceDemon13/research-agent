using System;

namespace Telemart.Client.Common.CustomTypeDescriptors
{
    public class PropertyGridRow
    {
        public PropertyGridRow()
        {
        }

        public PropertyGridRow(string name, string displayName, string groupName, int groupPosition, int propertyPosition, object value, Type type, bool isReadOnly)
        {
            Name = name;
            DisplayName = displayName;
            Value = value;
            Type = type;
            IsReadOnly = isReadOnly;
            GroupName = groupName;
            GroupPosition = groupPosition;
            PropertyPosition = propertyPosition;
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }

        public bool IsReadOnly { get; set; }

        public string GroupName { get; set; }

        public int GroupPosition { get; set; }

        public int PropertyPosition { get; set; }
    }
}