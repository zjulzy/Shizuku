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
    [ShizukuFunction("平方", "数学/基础", Pure = true)]
    public static float Square(float value)
    {
        return value * value;
    }

    [ShizukuFunction("平方根", "数学/基础", Pure = true)]
    public static float Sqrt(float value)
    {
        return Mathf.Sqrt(value);
    }

    [ShizukuFunction("限制范围", "数学/基础", Pure = true)]
    public static float Clamp01(float value)
    {
        return Mathf.Clamp01(value);
    }

    [ShizukuFunction("向量距离", "数学/向量", Pure = true)]
    public static float VectorDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    [ShizukuFunction("向量点乘", "数学/向量", Pure = true)]
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
    [ShizukuFunction("拼接字符串", "字符串/操作", Pure = true)]
    public static string Concat(string a, string b)
    {
        return string.Concat(a, b);
    }

    [ShizukuFunction("转大写", "字符串/转换", Pure = true)]
    public static string ToUpper(string str)
    {
        return str?.ToUpper();
    }

    [ShizukuFunction("转小写", "字符串/转换", Pure = true)]
    public static string ToLower(string str)
    {
        return str?.ToLower();
    }

    [ShizukuFunction("包含子串", "字符串/查询", Pure = true)]
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
    [ShizukuFunction("设置激活状态", "GameObject/操作")]
    public static void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    [ShizukuFunction("查找子物体", "GameObject/查询")]
    public static Transform FindChild(Transform parent, string childName)
    {
        return parent?.Find(childName);
    }

    [ShizukuFunction("销毁物体", "GameObject/操作")]
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
[ShizukuClass("敌人配置", "游戏/配置")]
[System.Serializable]
public class EnemyConfig
{
    public float MaxHealth = 100f;
    public float AttackPower = 10f;
    public float MoveSpeed = 5f;
    public Color NameColor = Color.red;
    
    [ShizukuFunction("计算伤害", "游戏/技能")]
    public float CalculateDamage(float baseDamage)
    {        
        return baseDamage * (1 + AttackPower / 100f);
    }
}

/// <summary>
/// 示例 5：技能数据
/// </summary>
[ShizukuClass("技能数据", "游戏/技能")]
[System.Serializable]
public class SkillData
{
    public string SkillName;
    public float Cooldown;
    public float ManaCost;
    public float Damage;
    public float Range;
}