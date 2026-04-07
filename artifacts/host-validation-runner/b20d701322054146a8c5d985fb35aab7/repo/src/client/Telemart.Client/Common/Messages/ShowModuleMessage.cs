using System;

namespace Telemart.Client.Common.Messages
{
    public class ShowModuleMessage
    {
        public ShowModuleMessage(Type viewType)
        {
            ViewType = viewType;
        }

        public Type ViewType { get; }
    }
}
