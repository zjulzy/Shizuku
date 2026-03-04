using System;
/// <summary>
/// ShizukuFunction Attribute - Mark methods to generate blueprint nodes
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ShizukuFunctionAttribute : Attribute
{
    public string DisplayName { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public bool Pure { get; set; }
    public int Order { get; set; }
    public Type[] GenericTypes { get; set; }
    public bool ShowInMenu { get; set; }
    public ShizukuFunctionAttribute(string displayName = null, string category = "Functions")
    {
        DisplayName = displayName;
        Category = category;
        Pure = false;
        Order = 0;
        ShowInMenu = true;
        GenericTypes = null;
    }
}