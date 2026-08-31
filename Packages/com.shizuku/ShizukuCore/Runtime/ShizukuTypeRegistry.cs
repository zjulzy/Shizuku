using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Shizuku.Core
{
    public static class ShizukuTypeRegistry
    {
        private static Dictionary<Type, ShizukuClassInfo> _registeredClasses = new Dictionary<Type, ShizukuClassInfo>();
        private static Dictionary<Type, List<ShizukuFunctionInfo>> _registeredFunctions = new Dictionary<Type, List<ShizukuFunctionInfo>>();
        private static List<ShizukuFunctionInfo> _allFunctions = new List<ShizukuFunctionInfo>();
        private static bool _initialized = false;
        static ShizukuTypeRegistry()
        {
            Initialize();
        }
    #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            _registeredClasses.Clear();
            _registeredFunctions.Clear();
            _allFunctions.Clear();
            _initialized = false;
            Initialize();
        }
    #endif
        public static void Initialize()
        {
            if (_initialized) return;
            ScanAndRegister();
            _initialized = true;
            Debug.Log($"[ShizukuTypeRegistry] Registered {_registeredClasses.Count} classes, {_allFunctions.Count} functions");
        }
        private static void ScanAndRegister()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (ShouldSkipAssembly(assembly)) continue;
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var classAttr = type.GetCustomAttribute<ShizukuClassAttribute>(true);
                        if (classAttr != null) RegisterClass(type, classAttr);
                        ScanFunctionsInType(type);
                    }
                }
                catch (ReflectionTypeLoadException) { continue; }
                catch (Exception ex) { Debug.LogWarning($"[ShizukuTypeRegistry] Error: {ex.Message}"); }
            }
        }
        private static bool ShouldSkipAssembly(Assembly assembly)
        {
            var name = assembly.FullName;
            return name.StartsWith("Unity") || name.StartsWith("System") || name.StartsWith("Mono") || 
                   name.StartsWith("mscorlib") || name.StartsWith("netstandard") || name.StartsWith("Microsoft");
        }
        private static void RegisterClass(Type type, ShizukuClassAttribute attr)
        {
            var info = new ShizukuClassInfo
            {
                Type = type,
                DisplayName = attr.DisplayName ?? type.Name,
                Category = attr.Category ?? "自定义",
                Description = attr.Description ?? "",
                ShowInVariableMenu = attr.ShowInVariableMenu,
                Order = attr.Order
            };
            _registeredClasses[type] = info;
        }
        private static void ScanFunctionsInType(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var funcAttr = method.GetCustomAttribute<ShizukuFunctionAttribute>(true);
                if (funcAttr != null) RegisterFunction(type, method, funcAttr);
            }
        }
        private static void RegisterFunction(Type type, MethodInfo method, ShizukuFunctionAttribute attr)
        {
            // 验证：ShizukuFunction 必须在 ShizukuClass 中
            if (!_registeredClasses.ContainsKey(type))
            {
                Debug.LogWarning($"[ShizukuTypeRegistry] ShizukuFunction '{method.Name}' in type '{type.Name}' is ignored because the type is not marked with [ShizukuClass]");
                return;
            }

            var info = new ShizukuFunctionInfo
            {
                DeclaringType = type,
                Method = method,
                DisplayName = attr.DisplayName ?? method.Name,
                Category = attr.Category ?? "函数",
                Description = attr.Description ?? "",
                IsPure = attr.Pure,
                Order = attr.Order,
                IsStatic = method.IsStatic,
                ReturnType = method.ReturnType,
                IsGenericMethod = method.IsGenericMethod,
                GenericTypes = attr.GenericTypes,
                ShowInMenu = attr.ShowInMenu
            };
            foreach (var param in method.GetParameters()) info.Parameters.Add(param);
            if (!_registeredFunctions.ContainsKey(type)) _registeredFunctions[type] = new List<ShizukuFunctionInfo>();
            _registeredFunctions[type].Add(info);
            _allFunctions.Add(info);
        }
        public static bool IsShizukuClass(Type type) => _registeredClasses.ContainsKey(type);
        public static IEnumerable<Type> GetAllShizukuClasses() => _registeredClasses.Keys;
        public static IEnumerable<ShizukuClassInfo> GetAllShizukuClassInfos() => _registeredClasses.Values;
        public static ShizukuClassInfo GetClassInfo(Type type) => _registeredClasses.TryGetValue(type, out var info) ? info : null;
        public static IEnumerable<ShizukuFunctionInfo> GetFunctionsForType(Type type) => _registeredFunctions.TryGetValue(type, out var functions) ? functions : Enumerable.Empty<ShizukuFunctionInfo>();
        public static IEnumerable<ShizukuFunctionInfo> GetAllFunctions() => _allFunctions;
        public static ShizukuFunctionInfo FindFunctionByNodeClassName(string nodeClassName) => _allFunctions.Find(f => f.GetNodeClassName() == nodeClassName);
        public static void Clear()
        {
            _registeredClasses.Clear();
            _registeredFunctions.Clear();
            _allFunctions.Clear();
            _initialized = false;
        }
    }
}
