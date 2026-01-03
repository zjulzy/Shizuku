// 示例：敌人行为类
// 步骤1: 先定义行为类（无需蓝图类存在，✅ 不会有编译错误）

using UnityEngine;

public class EnemyBehavior : BlueprintBehavior
{
    // 敌人属性
    [SerializeField]
    private float health = 100f;
    
    [SerializeField]
    private float speed = 5f;
    
    // 公开方法供蓝图调用
    public void SetHealth(float value) => health = value;
    public void SetSpeed(float value) => speed = value;
    
    public void Attack(GameObject target)
    {
        Debug.Log($"Enemy attacks {target.name} with health: {health}");
    }
}

// ===== 以下代码通过代码生成器自动生成 =====
// 步骤2: 右键 EnemyBehavior.cs → "Generate Blueprint Class"
// 生成器会：
// 1. 通过反射扫描 EnemyBehavior 的公开字段和方法
// 2. 生成 EnemyBlueprint : ShizukuBluePrint<EnemyBehavior>
// 3. 为每个可配置属性生成对应的蓝图字段

/*
// 自动生成的代码示例：

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBlueprint", menuName = "Shizuku/Blueprint/Enemy Blueprint")]
public class EnemyBlueprint : ShizukuBluePrint<EnemyBehavior>
{
    [Header("Enemy Configuration")]
    [SerializeField]
    private float configHealth = 100f;
    
    [SerializeField]
    private float configSpeed = 5f;
    
    public override void InitializeBehavior(EnemyBehavior behavior)
    {
        base.InitializeBehavior(behavior);
        
        // 使用蓝图配置初始化行为
        behavior.SetHealth(configHealth);
        behavior.SetSpeed(configSpeed);
        
        Debug.Log($"Initialized {behavior.GetType().Name} from blueprint: {BlueprintName}");
    }
}
*/

