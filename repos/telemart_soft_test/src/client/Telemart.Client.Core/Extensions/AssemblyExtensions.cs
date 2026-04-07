using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Telemart.Client.Core.Extensions
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetAccessibleTypes(this Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes.Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(t => t != null);
            }
        }

        public static string GetFileVersion(this Assembly assembly)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion;
        }

        public static IEnumerable<Type> GetTypesNestedFromGenericType(this Assembly assembly, Type genericType, Type genericTypeArg)
        {
            return GetTypesNestedFromGenericType(assembly, genericType, new[] { genericTypeArg });
        }

        public static IEnumerable<Type> GetTypesNestedFromGenericType(this Assembly assembly, Type genericType, Type[] genericTypeArgs)
        {
            IEnumerable<Type> q = from t in assembly.GetTypes()
                    where t.IsClass
                          && t.BaseType != null
                          && t.BaseType.IsGenericType
                          && t.BaseType.GetGenericTypeDefinition() == genericType
                          && t.BaseType.GetGenericArguments().SequenceEqual(genericTypeArgs)
                    select t;

            return q;
        }

        public static Type[] GetTypesImplementingInterface(this Assembly assembly, params Type[] interfaceTypes)
        {
            if (interfaceTypes == null || interfaceTypes.Length == 0)
            {
                return new Type[0];
            }

            return GetAccessibleTypes(assembly)
                .Where(t => t.IsClass && interfaceTypes.Any(i => i.IsAssignableFrom(t)))
                .ToArray();
        }

        public static Type[] GetTypesImplementingOpenGenericInterface(this Assembly assembly, params Type[] openGenericInterfaceTypes)
        {
            if (openGenericInterfaceTypes == null || openGenericInterfaceTypes.Length == 0)
            {
                return new Type[0];
            }

            return GetAccessibleTypes(assembly)
                .Where(t => t.IsClass && t.GetInterfaces().Any(y => y.IsGenericType && openGenericInterfaceTypes.Contains(y.GetGenericTypeDefinition())))
                .ToArray();
        }
    }
}