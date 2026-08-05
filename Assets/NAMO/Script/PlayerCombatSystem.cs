using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform upAttackPoint;
    [SerializeField] private Transform downAttackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRate = 3.5f;

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

    // Fixed pre-allocated array for low overhead in Unity 6
    private readonly Collider2D[] hitEnemiesBuffer = new Collider2D[10];

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlameInfusion();
        }

        if (isFlameInfused)
        {
            currentFlameEnergy -= flameEnergyCostPerSec * Time.deltaTime;
            if (currentFlameEnergy <= 0f)
            {
                currentFlameEnergy = 0f;
                isFlameInfused = false;
            }
        }

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
            AttackDown();
        }
        else
        {
            AttackDirection(attackPoint, $"Attack_Combo_{comboStep + 1}");
            comboStep = (comboStep + 1) % 3;
        }
    }

    private void AttackDirection(Transform point, string attackAnimName)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(point.position, attackRange, hitEnemiesBuffer, enemyLayer);
        int finalDamage = baseDamage + (isFlameInfused ? flameDamageBonus : 0);

        for (int i = 0; i < hitCount; i++)
        {
            Debug.Log($"Hit {hitEnemiesBuffer[i].name} for {finalDamage} damage! (Flame Mode: {isFlameInfused})");
        }
    }

    private void AttackDown()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(downAttackPoint.position, attackRange, hitEnemiesBuffer, enemyLayer);

        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Debug.Log($"Pogo Hit {hitEnemiesBuffer[i].name}!");
            }
            controller.Bounce(pogoBounceForce);
        }
    }

    private void ToggleFlameInfusion()
    {
        if (currentFlameEnergy > 10f)
        {
            isFlameInfused = !isFlameInfused;
            Debug.Log($"Pure Flame Infusion State: {isFlameInfused}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        if (upAttackPoint) Gizmos.DrawWireSphere(upAttackPoint.position, attackRange);
        if (downAttackPoint) Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
    }
}