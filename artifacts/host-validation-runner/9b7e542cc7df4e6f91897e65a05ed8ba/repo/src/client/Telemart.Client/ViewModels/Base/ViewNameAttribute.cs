using System;

namespace Telemart.Client.ViewModels.Base
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewNameAttribute : Attribute
    {
        public ViewNameAttribute(string viewName)
        {
            ViewName = viewName;
        }

        public string ViewName { get; }
    }
}
