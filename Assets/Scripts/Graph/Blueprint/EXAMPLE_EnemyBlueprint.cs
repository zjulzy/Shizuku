using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 敌人行为类示例
/// 演示如何使用 [BlueprintOverridable] 简化蓝图重写逻辑
/// </summary>
public class EXAMPLE_EnemyBlueprint : BlueprintBehavior<EXAMPLE_EnemyBlueprint>
{
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected float defense = 10f;

    [BlueprintOverridable]
    [Button]
    protected virtual void OnDeath()
    {
        if (TryExecuteBlueprintOverride(nameof(OnDeath)))
            return;
        
        Debug.Log($"{gameObject.name} died!");
        // Destroy(gameObject);
    }

    [BlueprintOverridable("Attack")]
    [Button]
    public virtual void Attack(float damage)
    {
        if (TryExecuteBlueprintOverride(nameof(Attack), damage))
            return;
        
        Debug.Log($"{gameObject.name} attacks");
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
}
