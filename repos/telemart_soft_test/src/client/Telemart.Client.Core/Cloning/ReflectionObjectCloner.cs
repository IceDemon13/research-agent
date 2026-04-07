using System;
using System.Reflection;

namespace Telemart.Client.Core.Cloning
{
    public static class ReflectionObjectCloner
    {
        public static T Clone<T>(T source, Func<T> targetFactory = null)
        where T : class
        {
            if (source is null)
            {
                return null;
            }

            Type type = source.GetType();

            T target = targetFactory == null
                ? (T)Activator.CreateInstance(type)
                : targetFactory();

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name.Equals("Error", StringComparison.Ordinal) || !propertyInfo.CanWrite)
                {
                    continue;
                }

                object value = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, value);
            }

            return target;
        }
    }
}