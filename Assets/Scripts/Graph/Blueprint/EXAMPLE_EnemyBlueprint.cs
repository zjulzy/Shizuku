using UnityEngine;
public class EnemyBehavior : BlueprintBehavior
{
    [SerializeField]
    protected float health = 100f;
    [SerializeField]
    protected float speed = 5f;
    [SerializeField]
    protected float defense = 10f;
    public virtual void TakeDamage(float damage)
    {
        if (IsBlueprintEventImplemented("OnTakeDamage"))
        {
            ExecuteBlueprintEvent("OnTakeDamage", damage);
        }
        else
        {
            DefaultTakeDamage(damage);
        }
    }
    protected virtual void DefaultTakeDamage(float damage)
    {
        float actualDamage = Mathf.Max(0, damage - defense);
        health -= actualDamage;
        Debug.Log($"{gameObject.name} took {actualDamage} damage. Health: {health}");
        if (health <= 0)
        {
            OnDeath();
        }
    }
    protected virtual void OnDeath()
    {
        if (IsBlueprintEventImplemented("OnDeath"))
        {
            ExecuteBlueprintEvent("OnDeath");
        }
        else
        {
            Debug.Log($"{gameObject.name} died!");
            Destroy(gameObject);
        }
    }
    public virtual void Attack(GameObject target)
    {
        if (IsBlueprintEventImplemented("OnAttack"))
        {
            ExecuteBlueprintEvent("OnAttack", target);
        }
        else
        {
            Debug.Log($"{gameObject.name} attacks {target.name}");
        }
    }
    public void Heal(float amount)
    {
        health += amount;
        health = Mathf.Min(health, 100f);
        Debug.Log($"{gameObject.name} healed {amount}. Health: {health}");
    }
    public void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
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
        behavior.RegisterReadOnlyProperty("IsAlive", () => behavior.GetBlueprintProperty("health") is float h && h > 0);
    }
}
