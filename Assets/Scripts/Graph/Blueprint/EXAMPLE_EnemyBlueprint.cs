using UnityEngine;

/// <summary>
/// 敌人行为类示例
/// 演示如何使用 [BlueprintOverridable] 简化蓝图重写逻辑
/// </summary>
public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
{
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected float defense = 10f;

    [BlueprintOverridable]
    public virtual void TakeDamage(float damage)
    {
        if (TryExecuteBlueprintOverride(nameof(TakeDamage), damage))
            return;
        
        // 默认逻辑
        float actualDamage = Mathf.Max(0, damage - defense);
        health -= actualDamage;
        Debug.Log($"{gameObject.name} took {actualDamage} damage. Health: {health}");
        
        if (health <= 0)
        {
            OnDeath();
        }
    }

    [BlueprintOverridable]
    protected virtual void OnDeath()
    {
        if (TryExecuteBlueprintOverride(nameof(OnDeath)))
            return;
        
        Debug.Log($"{gameObject.name} died!");
        Destroy(gameObject);
    }

    [BlueprintOverridable("OnAttack")]
    public virtual void Attack(GameObject target)
    {
        if (TryExecuteBlueprintOverride(nameof(Attack), target))
            return;
        
        Debug.Log($"{gameObject.name} attacks {target.name}");
    }

    // 不标记 Attribute 的方法不会被蓝图重写
    public void Heal(float amount)
    {
        health += amount;
        health = Mathf.Min(health, 100f);
        Debug.Log($"{gameObject.name} healed {amount}. Health: {health}");
    }

    public void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(
            transform.position, target, speed * Time.deltaTime);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            // 自动判断：蓝图实现了就用蓝图，否则用 C# 逻辑
            TakeDamage(20f);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(15f);
        }
    }
}

[CreateAssetMenu(fileName = "EnemyBlueprint", menuName = "Shizuku/Blueprint/Enemy Blueprint")]
public class EnemyBlueprint : ShizukuBluePrint<EnemyBehavior>
{
    public override void InitializeBehavior(EnemyBehavior behavior)
    {
        base.InitializeBehavior(behavior);
        Debug.Log($"EnemyBlueprint initialized for {behavior.gameObject.name}");
    }
}
