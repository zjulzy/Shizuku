using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shizuku.Core
{
    /// <summary>
    /// ShizukuClass 的元数据信息
    /// </summary>
    public class ShizukuClassInfo
    {
        public Type Type { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool ShowInVariableMenu { get; set; }
        public int Order { get; set; }
        public List<FieldInfo> ExposedFields { get; set; }
        public List<PropertyInfo> ExposedProperties { get; set; }

        public ShizukuClassInfo()
        {
            ExposedFields = new List<FieldInfo>();
            ExposedProperties = new List<PropertyInfo>();
        }
    }

    /// <summary>
    /// ShizukuFunction 的元数据信息
    /// </summary>
    public class ShizukuFunctionInfo
    {
        public Type DeclaringType { get; set; }
        public MethodInfo Method { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool IsPure { get; set; }
        public int Order { get; set; }
        public bool IsStatic { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
        public Type ReturnType { get; set; }
        public bool IsGenericMethod { get; set; }
        public Type[] GenericTypes { get; set; }
        public bool ShowInMenu { get; set; }

        public ShizukuFunctionInfo()
        {
            Parameters = new List<ParameterInfo>();
        }

        public string GetNodeClassName()
        {
            var className = DeclaringType.Name.Replace("`", "_");
            var methodName = Method.Name.Replace("`", "_");

            var paramSignature = "";
            if (Parameters.Count > 0)
            {
                var paramTypes = new List<string>();
                foreach (var p in Parameters)
                {
                    paramTypes.Add(GetSimpleTypeName(p.ParameterType));
                }
                paramSignature = "_" + string.Join("_", paramTypes);
            }

            return $"{className}_{methodName}{paramSignature}_Node";
        }

        public string GetMenuPath()
        {
            if (!string.IsNullOrEmpty(Category))
            {
                return $"{Category}/{DisplayName}";
            }
            return $"函数/{DisplayName}";
        }

        private string GetSimpleTypeName(Type type)
        {
            if (type == typeof(int)) return "Int";
            if (type == typeof(float)) return "Float";
            if (type == typeof(bool)) return "Bool";
            if (type == typeof(string)) return "String";
            if (type.IsArray) return GetSimpleTypeName(type.GetElementType()) + "Array";
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                var genericNames = new List<string>();
                foreach (var arg in genericArgs)
                {
                    genericNames.Add(GetSimpleTypeName(arg));
                }
                return type.Name.Split('`')[0] + string.Join("", genericNames);
            }
            return type.Name;
        }
    }


}
