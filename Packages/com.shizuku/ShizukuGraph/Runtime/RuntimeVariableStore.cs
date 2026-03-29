using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    /// <summary>
    /// 运行时变量存储容器
    /// 内置类型零装箱，自定义类型通过类型化字典注册表实现零装箱
    /// </summary>
    public class RuntimeVariableStore
    {
        // 内置类型字典
        public Dictionary<string, int> Ints = new();
        public Dictionary<string, float> Floats = new();
        public Dictionary<string, bool> Bools = new();
        public Dictionary<string, string> Strings = new();
        public Dictionary<string, Vector2> Vector2s = new();
        public Dictionary<string, Vector3> Vector3s = new();
        public Dictionary<string, GameObject> GameObjects = new();
        public Dictionary<string, Transform> Transforms = new();
        public Dictionary<string, Color> Colors = new();

        /// <summary>
        /// 自定义类型字典注册表
        /// key = Type, value = Dictionary&lt;string, T&gt;（强类型，零装箱）
        /// </summary>
        private Dictionary<Type, IDictionary> _customDicts = new();

        /// <summary>
        /// 获取或创建指定类型的字典（零装箱访问）
        /// </summary>
        public Dictionary<string, T> GetOrCreateCustomDict<T>()
        {
            var type = typeof(T);
            if (!_customDicts.TryGetValue(type, out var dict))
            {
                var typed = new Dictionary<string, T>();
                _customDicts[type] = typed;
                return typed;
            }
            return (Dictionary<string, T>)dict;
        }

        public void Init()
        {
            Ints = new();
            Floats = new();
            Bools = new();
            Strings = new();
            Vector2s = new();
            Vector3s = new();
            GameObjects = new();
            Transforms = new();
            Colors = new();
            _customDicts = new();
        }

        public void LoadFromVariables(List<GraphVariable> variables)
        {
            foreach (var variable in variables)
            {
                switch (variable.Type)
                {
                    case VariableType.Int: 
                        Ints[variable.GUID] = variable.IntValue; 
                        break;
                    case VariableType.Float:
                        Floats[variable.GUID] = variable.FloatValue;
                        break;
                    case VariableType.Bool: 
                        Bools[variable.GUID] = variable.BoolValue;
                        break;
                    case VariableType.String: 
                        Strings[variable.GUID] = variable.StringValue;
                        break;
                    case VariableType.Vector2: 
                        Vector2s[variable.GUID] = variable.Vector2Value; 
                        break;
                    case VariableType.Vector3: 
                        Vector3s[variable.GUID] = variable.Vector3Value; 
                        break;
                    case VariableType.GameObject: 
                        GameObjects[variable.GUID] = variable.GameObjectValue; 
                        break;
                    case VariableType.Transform: 
                        Transforms[variable.GUID] = variable.TransformValue; 
                        break;
                    case VariableType.Color: 
                        Colors[variable.GUID] = variable.ColorValue;
                        break;
                    case VariableType.Custom: 
                        LoadCustomVariable(variable); 
                        break;
                }
            }
        }

        /// <summary>
        /// 加载自定义类型变量：根据 CustomValue 的实际类型存入对应字典
        /// </summary>
        private void LoadCustomVariable(GraphVariable variable)
        {
            if (variable.CustomValue == null) return;

            var valueType = variable.CustomValue.GetType();
            if (!_customDicts.TryGetValue(valueType, out var dict))
            {
                // 通过反射创建 Dictionary<string, T>
                var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                dict = (IDictionary)Activator.CreateInstance(dictType);
                _customDicts[valueType] = dict;
            }
            dict[variable.GUID] = variable.CustomValue;
        }

        public RuntimeVariableStore Clone()
        {
            var clone = new RuntimeVariableStore
            {
                Ints = new Dictionary<string, int>(Ints),
                Floats = new Dictionary<string, float>(Floats),
                Bools = new Dictionary<string, bool>(Bools),
                Strings = new Dictionary<string, string>(Strings),
                Vector2s = new Dictionary<string, Vector2>(Vector2s),
                Vector3s = new Dictionary<string, Vector3>(Vector3s),
                GameObjects = new Dictionary<string, GameObject>(GameObjects),
                Transforms = new Dictionary<string, Transform>(Transforms),
                Colors = new Dictionary<string, Color>(Colors),
            };

            // 克隆自定义类型字典（浅拷贝每个字典）
            foreach (var kvp in _customDicts)
            {
                var srcDict = kvp.Value;
                var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), kvp.Key);
                var newDict = (IDictionary)Activator.CreateInstance(dictType, srcDict);
                clone._customDicts[kvp.Key] = newDict;
            }

            return clone;
        }
    }

}
