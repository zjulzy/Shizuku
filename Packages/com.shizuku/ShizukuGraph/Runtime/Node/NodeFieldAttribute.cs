using System;

namespace Shizuku.Graph
{
    /// <summary>Editor presentation only; never changes serialized field or port identifiers.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NodeFieldAttribute : Attribute
    {
        public string Label { get; }
        public string Unit { get; set; }
        public bool Summary { get; set; }
        public bool Required { get; set; }
        public NodeFieldAttribute(string label) { Label = label; }
    }
}
