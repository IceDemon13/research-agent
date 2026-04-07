using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Telemart.Client.Common.CustomTypeDescriptors
{
    public class CollectionTypeDescriptor : CustomTypeDescriptor
    {
        public CollectionTypeDescriptor(IReadOnlyCollection<PropertyGridRow> rows)
        {
            Rows = rows;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            return new PropertyDescriptorCollection(Rows.Select(x => new PropertyGridRowPropertyDescriptor(x)).Cast<PropertyDescriptor>().ToArray());
        }

        public override object? GetPropertyOwner(PropertyDescriptor? pd) => this;

        public IReadOnlyCollection<PropertyGridRow> Rows { get; set; }
    }
}