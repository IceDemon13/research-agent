using System;
using System.Reflection;

namespace Telemart.Client.Core.Extensions
{
    public static class TypeExtensions
    {
        public static object CreateObject(this Type targetType, Type[] parameterTypes, object[] parameters)
        {
            ConstructorInfo constructor = targetType.GetConstructor(parameterTypes);

            if (constructor == null)
            {
                throw new InvalidOperationException("Constructor with such signature not found");
            }

            return constructor.Invoke(parameters);
        }
    }
}