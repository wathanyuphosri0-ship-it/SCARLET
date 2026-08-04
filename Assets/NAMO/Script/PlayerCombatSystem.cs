using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform upAttackPoint;
    [SerializeField] private Transform downAttackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRate = 3.5f; // จำนวนครั้งต่อวินาที

    [Header("Damage Settings")]
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private int flameDamageBonus = 10;
    [SerializeField] private float pogoBounceForce = 14f;

    [Header("Pure Flame Mode")]
    [SerializeField] private bool isFlameInfused = false;
    [SerializeField] private float flameEnergyCostPerSec = 5f;
    [SerializeField] private float currentFlameEnergy = 100f;
    [SerializeField] private float maxFlameEnergy = 100f;

    private PlayerController2D controller;
    private float nextAttackTime = 0f;
    private int comboStep = 0;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
    }

    private void Update()
    {
        // Toggle Pure Flame Infusion (กด F เพื่อเปิด/ปิดโหมดไฟบริสุทธิ์)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlameInfusion();
        }

        // Drain Flame Energy เมื่อเปิดโหมด
        if (isFlameInfused)
        {
            currentFlameEnergy -= flameEnergyCostPerSec * Time.deltaTime;
            if (currentFlameEnergy <= 0f)
            {
                currentFlameEnergy = 0f;
                isFlameInfused = false; // พลังหมด ปิดโหมดอัตโนมัติ
            }
        }

        // Attack Inputs (กด J หรือ คลิกซ้าย เพื่อฟัน)
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J))
            {
                PerformAttack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput > 0)
        {
            AttackDirection(upAttackPoint, "AttackUp");
        }
        else if (verticalInput < 0 && !GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            // ฟันลงลอยกลางอากาศ (Down Attack / Pogo)
            AttackDown();
        }
        else
        {
            // Normal / Combo Attack
            AttackDirection(attackPoint, $"Attack_Combo_{comboStep + 1}");
            comboStep = (comboStep + 1) % 3; // สลับ Combo 1 -> 2 -> 3
        }
    }

    private void AttackDirection(Transform point, string attackAnimName)
    {
        // TODO: สั่งเล่น Animation ตามชื่อ attackAnimName (ส่งให้ Animator)
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point.position, attackRange, enemyLayer);
        int finalDamage = baseDamage + (isFlameInfused ? flameDamageBonus : 0);

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"Hit {enemy.name} for {finalDamage} damage! (Flame Mode: {isFlameInfused})");
            // enemy.GetComponent<EnemyHealth>()?.TakeDamage(finalDamage);
        }
    }

    private void AttackDown()
    {
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(downAttackPoint.position, attackRange, enemyLayer);
        int finalDamage = baseDamage + (isFlameInfused ? flameDamageBonus : 0);

        if (hitTargets.Length > 0)
        {
            foreach (Collider2D target in hitTargets)
            {
                Debug.Log($"Pogo Hit {target.name}!");
            }
            // Pogo Bounce: เด้งตัว Scarlet ขึ้นกลางอากาศเมื่อฟันโดน
            controller.Bounce(pogoBounceForce);
        }
    }

    private void ToggleFlameInfusion()
    {
        if (currentFlameEnergy > 10f)
        {
            isFlameInfused = !isFlameInfused;
            Debug.Log($"Pure Flame Infusion: {isFlameInfused}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        if (upAttackPoint) Gizmos.DrawWireSphere(upAttackPoint.position, attackRange);
        if (downAttackPoint) Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
    }
}