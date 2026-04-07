using System;
using System.Threading.Tasks;

namespace Telemart.Client.Extensions
{
    public static class ObjectExtensions
    {
        public static bool Same(this object obj1, object obj2)
        {
            bool result;

            IComparable selfValueComparer = obj1 as IComparable;

            if ((obj1 == null && obj2 != null) || (obj1 != null && obj2 == null))
            {
                result = false; // one of the values is null
            }
            else if (selfValueComparer != null && selfValueComparer.CompareTo(obj2) != 0)
            {
                result = false; // the comparison using IComparable failed
            }
            else if (!object.Equals(obj1, obj2))
            {
                result = false; // the comparison using Equals failed
            }
            else
            {
                result = true; // match
            }

            return result;
        }

        public static T IfNotNull<T>(this T obj, Action<T> action)
            where T : class
        {
            if (obj != null && action != null)
            {
                action(obj);
            }

            return obj;
        }

        public static string ToStringAlt(this bool value)
        {
            return value ? "✓" : "✗";
        }

        public static Task<T> IfNotNullAsync<T>(this T obj, Func<T, Task<T>> action)
            where T : class
        {
            if (obj != null && action != null)
            {
                return action(obj);
            }

            return Task.FromResult(obj);
        }
    }
}