using System;

namespace Telemart.Client.Core.Extensions
{
    public static class RandomExtensions
    {
        public static int GetRandomId(this Random random)
        {
            return -1 * random.Next(1, int.MaxValue);
        }
    }
}