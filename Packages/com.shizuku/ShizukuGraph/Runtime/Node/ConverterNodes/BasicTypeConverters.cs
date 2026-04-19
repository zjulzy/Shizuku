using System;
using UnityEngine;

/// <summary>
/// Float → Int 转换节点
/// 执行截断转换（向零取整）
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("数值/Float → Int", NodeCategory.Converter, Description = "浮点数转整数（截断取整）")]
    public class FloatToIntConverter : TypeConverterNode
    {
        [SerializeReference]
        private FloatParameterEdgePort _input = new FloatParameterEdgePort { IsOut = false, Name = "Float" };

        [SerializeReference]
        private IntParameterEdgePort _output = new IntParameterEdgePort { IsOut = true, Name = "Int" };

        public override string Title => "Float → Int";

        public override Type InputType => typeof(float);
        public override Type OutputType => typeof(int);

        protected override void ConvertValue()
        {
            _output.Value = (int)_input.Value;
        }
    }

    /// <summary>
    /// Int → Float 转换节点
    /// </summary>
    [Serializable]
    [NodeMenuItem("数值/Int → Float", NodeCategory.Converter, Description = "整数转浮点数")]
    public class IntToFloatConverter : TypeConverterNode
    {
        [SerializeReference]
        private IntParameterEdgePort _input = new IntParameterEdgePort { IsOut = false, Name = "Int" };

        [SerializeReference]
        private FloatParameterEdgePort _output = new FloatParameterEdgePort { IsOut = true, Name = "Float" };

        public override string Title => "Int → Float";

        public override Type InputType => typeof(int);
        public override Type OutputType => typeof(float);

        protected override void ConvertValue()
        {
            _output.Value = (float)_input.Value;
        }
    }

    /// <summary>
    /// Int → Bool 转换节点
    /// 0 → false, 非零 → true
    /// </summary>
    [Serializable]
    [NodeMenuItem("数值/Int → Bool", NodeCategory.Converter, Description = "整数转布尔（0为false，非零为true）")]
    public class IntToBoolConverter : TypeConverterNode
    {
        [SerializeReference]
        private IntParameterEdgePort _input = new IntParameterEdgePort { IsOut = false, Name = "Int" };

        [SerializeReference]
        private BoolParameterEdgePort _output = new BoolParameterEdgePort { IsOut = true, Name = "Bool" };

        public override string Title => "Int → Bool";

        public override Type InputType => typeof(int);
        public override Type OutputType => typeof(bool);

        protected override void ConvertValue()
        {
            _output.Value = _input.Value != 0;
        }
    }

    /// <summary>
    /// Bool → Int 转换节点
    /// false → 0, true → 1
    /// </summary>
    [Serializable]
    [NodeMenuItem("数值/Bool → Int", NodeCategory.Converter, Description = "布尔转整数（false为0，true为1）")]
    public class BoolToIntConverter : TypeConverterNode
    {
        [SerializeReference]
        private BoolParameterEdgePort _input = new BoolParameterEdgePort { IsOut = false, Name = "Bool" };

        [SerializeReference]
        private IntParameterEdgePort _output = new IntParameterEdgePort { IsOut = true, Name = "Int" };

        public override string Title => "Bool → Int";

        public override Type InputType => typeof(bool);
        public override Type OutputType => typeof(int);

        protected override void ConvertValue()
        {
            _output.Value = _input.Value ? 1 : 0;
        }
    }

    /// <summary>
    /// Float → String 转换节点
    /// </summary>
    [Serializable]
    [NodeMenuItem("字符串/Float → String", NodeCategory.Converter, Description = "浮点数转字符串")]
    public class FloatToStringConverter : TypeConverterNode
    {
        [SerializeReference]
        private FloatParameterEdgePort _input = new FloatParameterEdgePort { IsOut = false, Name = "Float" };

        [SerializeReference]
        private StringParameterEdgePort _output = new StringParameterEdgePort { IsOut = true, Name = "String" };

        public override string Title => "Float → String";

        public override Type InputType => typeof(float);
        public override Type OutputType => typeof(string);

        protected override void ConvertValue()
        {
            _output.Value = _input.Value.ToString("F2");
        }
    }

    /// <summary>
    /// Int → String 转换节点
    /// </summary>
    [Serializable]
    [NodeMenuItem("字符串/Int → String", NodeCategory.Converter, Description = "整数转字符串")]
    public class IntToStringConverter : TypeConverterNode
    {
        [SerializeReference]
        private IntParameterEdgePort _input = new IntParameterEdgePort { IsOut = false, Name = "Int" };

        [SerializeReference]
        private StringParameterEdgePort _output = new StringParameterEdgePort { IsOut = true, Name = "String" };

        public override string Title => "Int → String";

        public override Type InputType => typeof(int);
        public override Type OutputType => typeof(string);

        protected override void ConvertValue()
        {
            _output.Value = _input.Value.ToString();
        }
    }

    /// <summary>
    /// String → Int 转换节点
    /// 解析失败返回 0
    /// </summary>
    [Serializable]
    [NodeMenuItem("字符串/String → Int", NodeCategory.Converter, Description = "字符串转整数（失败返回0）")]
    public class StringToIntConverter : TypeConverterNode
    {
        [SerializeReference]
        private StringParameterEdgePort _input = new StringParameterEdgePort { IsOut = false, Name = "String" };

        [SerializeReference]
        private IntParameterEdgePort _output = new IntParameterEdgePort { IsOut = true, Name = "Int" };

        public override string Title => "String → Int";

        public override Type InputType => typeof(string);
        public override Type OutputType => typeof(int);

        protected override void ConvertValue()
        {
            if (int.TryParse(_input.Value, out var result))
            {
                _output.Value = result;
            }
            else
            {
                _output.Value = 0;
            }
        }
    }

    /// <summary>
    /// String → Float 转换节点
    /// 解析失败返回 0.0f
    /// </summary>
    [Serializable]
    [NodeMenuItem("字符串/String → Float", NodeCategory.Converter, Description = "字符串转浮点数（失败返回0）")]
    public class StringToFloatConverter : TypeConverterNode
    {
        [SerializeReference]
        private StringParameterEdgePort _input = new StringParameterEdgePort { IsOut = false, Name = "String" };

        [SerializeReference]
        private FloatParameterEdgePort _output = new FloatParameterEdgePort { IsOut = true, Name = "Float" };

        public override string Title => "String → Float";

        public override Type InputType => typeof(string);
        public override Type OutputType => typeof(float);

        protected override void ConvertValue()
        {
            if (float.TryParse(_input.Value, out var result))
            {
                _output.Value = result;
            }
            else
            {
                _output.Value = 0f;
            }
        }
    }


}
