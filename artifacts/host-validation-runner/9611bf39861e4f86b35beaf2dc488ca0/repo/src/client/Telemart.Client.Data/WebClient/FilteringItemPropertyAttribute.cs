using System;

namespace Telemart.Client.Data.WebClient
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FilteringItemPropertyAttribute : Attribute
    {
        public FilteringItemPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}