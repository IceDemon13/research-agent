using System;

namespace Telemart.Client.Core.Update
{
    public class CheckUpdateException : Exception
    {
        public CheckUpdateException(string message)
            : base(message)
        {
        }
    }
}