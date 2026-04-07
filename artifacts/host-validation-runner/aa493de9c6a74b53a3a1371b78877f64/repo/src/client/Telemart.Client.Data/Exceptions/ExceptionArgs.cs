using System;

namespace Telemart.Client.Core.Exceptions
{
    [Serializable]
    public abstract class ExceptionArgs
    {
        public virtual string Message => string.Empty;
    }
}