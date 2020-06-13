using SwaggerWcf.Attributes;
using SwaggerWcf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SwaggerWcf.Support
{
    internal static class TypeExtensions
    {
        public static Type GetEnumerableType(this Type type)
        {
            Type elementType = type.GetElementType();
            if (elementType != null)
                return elementType;

            Type[] genericArguments = type.GetGenericArguments();

            return genericArguments.Any() ? genericArguments[0] : null;
        }

        private static readonly Dictionary<string, Type> _existingTypesByName = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> _existingTypeNames = new Dictionary<Type, string>();
        private static readonly object _typesLock = new object();

        public static string GetModelName(this Type type)
        {
            if (_existingTypeNames.TryGetValue(type, out string typeName))
            {
                return typeName;
            }

            typeName = type.GetCustomAttribute<SwaggerWcfDefinitionAttribute>()?.ModelName;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                if (_existingTypeNames.TryGetValue(type, out typeName))
                    return typeName;

                lock (_typesLock)
                {
                    if (_existingTypeNames.TryGetValue(type, out typeName))
                        return typeName;

                    typeName = type.ToNiceName();
                    //var newTypeName = type.ToNiceName();
                    int loopLimit = 100;   // no retries more than 
                    int loopCounter = 0;
                    while(_existingTypesByName.ContainsKey(typeName) && loopCounter < loopLimit)
                    {
                        typeName = type.ToNiceName(++loopCounter);
                    }

                    // Fallback to Full type name.
                    if (_existingTypesByName.ContainsKey(typeName))
                        typeName = type.FullName;

                    _existingTypeNames.Add(type, typeName);
                    _existingTypesByName.Add(typeName, type);
                }
            }

            return typeName;
        }

        public static string ToNiceName(this Type type, int iteration = 0)
        {
            if (type == null)
                return string.Empty;

            if (!type.IsGenericType)
                return iteration > 0 ? $"{type.Name}{iteration}" : type.Name;

            var name = type.GetGenericTypeDefinition().Name;
            name = name.Remove(name.IndexOf('`'));
            if (iteration > 0)
                name += iteration;

            var genericParams = new StringBuilder();
            foreach (var paramType in type.GenericTypeArguments)
            {
                var paramName = GetModelName(paramType);
                genericParams.Append(paramName);
                genericParams.Append(", ");
            }


            if (genericParams.Length > 0)
            {
                genericParams = genericParams.Remove(genericParams.Length - 2, 2);  // remove last ", "
            }

            return string.Format("{0}[{1}]", name, genericParams);
        }

        public static string GetModelWrappedName(this Type type) =>
            type.GetCustomAttribute<SwaggerWcfDefinitionAttribute>()?.ModelName ?? type.FullName;

        internal static Info GetServiceInfo(this TypeInfo typeInfo)
        {
            var infoAttr = typeInfo.GetCustomAttribute<SwaggerWcfServiceInfoAttribute>() ??
                throw new ArgumentException($"{typeInfo.FullName} does not have {nameof(SwaggerWcfServiceInfoAttribute)}");

            var info = (Info)infoAttr;

            var contactAttr = typeInfo.GetCustomAttribute<SwaggerWcfContactInfoAttribute>();
            if (contactAttr != null)
            {
                info.Contact = (InfoContact)contactAttr;
            }

            var licenseAttr = typeInfo.GetCustomAttribute<SwaggerWcfLicenseInfoAttribute>();
            if (licenseAttr != null)
            {
                info.License = (InfoLicense)licenseAttr;
            }

            return info;
        }
    }
}
