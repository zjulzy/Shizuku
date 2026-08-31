using UnityEngine;
using Shizuku.Core;
using Shizuku.Graph;

/// <summary>
/// ShizukuClass 和 ShizukuFunction 使用示例
/// </summary>
/// <summary>
/// 示例 1：数学工具类
/// </summary>
[ShizukuClass]
public static class MathUtils
{
    [ShizukuFunction("Square", "数学", Pure = true)]
    public static float Square(float value)
    {
        return value * value;
    }

    [ShizukuFunction("Square Root", "数学", Pure = true)]
    public static float Sqrt(float value)
    {
        return Mathf.Sqrt(value);
    }

    [ShizukuFunction("Clamp 01", "数学", Pure = true)]
    public static float Clamp01(float value)
    {
        return Mathf.Clamp01(value);
    }

    [ShizukuFunction("Vector Distance", "数学", Pure = true)]
    public static float VectorDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    [ShizukuFunction("Vector Dot", "数学", Pure = true)]
    public static float VectorDot(Vector3 a, Vector3 b)
    {
        return Vector3.Dot(a, b);
    }
}

/// <summary>
/// 示例 2：字符串工具类
/// </summary>
[ShizukuClass]
public static class StringUtils
{
    [ShizukuFunction("Concat", "字符串", Pure = true)]
    public static string Concat(string a, string b)
    {
        return string.Concat(a, b);
    }

    [ShizukuFunction("To Upper", "字符串", Pure = true)]
    public static string ToUpper(string str)
    {
        return str?.ToUpper();
    }

    [ShizukuFunction("To Lower", "字符串", Pure = true)]
    public static string ToLower(string str)
    {
        return str?.ToLower();
    }

    [ShizukuFunction("Contains", "字符串", Pure = true)]
    public static bool Contains(string str, string substring)
    {
        return str?.Contains(substring) ?? false;
    }
}

/// <summary>
/// 示例 3：GameObject 工具类
/// </summary>
public static class GameObjectUtils
{
    [ShizukuFunction("Set Active", "游戏对象")]
    public static void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    [ShizukuFunction("Find Child", "游戏对象")]
    public static Transform FindChild(Transform parent, string childName)
    {
        return parent?.Find(childName);
    }

    [ShizukuFunction("Destroy Object", "游戏对象")]
    public static void DestroyObject(GameObject obj)
    {
        if (obj != null)
        {
            Object.Destroy(obj);
        }
    }
}


/// <summary>
/// 示例 4：自定义类型作为 ShizukuClass
/// </summary>
[ShizukuClass("Enemy Config", "示例/Enemy Config")]
[System.Serializable]
public class EnemyConfig
{
    public float MaxHealth = 100f;
    public float AttackPower = 10f;
    public float MoveSpeed = 5f;
    public Color NameColor = Color.red;
    
    [ShizukuFunction("Calculate Damage", "敌人配置")]
    public float CalculateDamage(float baseDamage)
    {        
        return baseDamage * (1 + AttackPower / 100f);
    }
}

/// <summary>
/// 示例 5：技能数据
/// </summary>
[ShizukuClass("Skill Data", "示例/Skill Data")]
[System.Serializable]
public class SkillData
{
    public string SkillName;
    public float Cooldown;
    public float ManaCost;
    public float Damage;
    public float Range;
}
