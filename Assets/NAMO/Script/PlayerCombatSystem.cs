using System.Collections;
using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Hitbox GameObjects")]
    [SerializeField] private GameObject forwardHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;
    [SerializeField] private float hitboxActiveTime = 0.2f;

    [Header("Base Attack Settings")]
    [SerializeField] private float baseAttackRate = 3.5f;
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private float pogoBounceForce = 14f;

    [Header("Pure Flame Mode - Energy & Buffs")]
    [SerializeField] private bool isFlameInfused = false;
    [SerializeField] private float currentFlameEnergy = 0f;
    [SerializeField] private float maxFlameEnergy = 100f;
    [SerializeField] private float minEnergyToActivate = 30f;
    [SerializeField] private float passiveRegenRate = 5f; // รีเจนเพิ่มขึ้นเองต่อวินาที
    [SerializeField] private float energyCostPerSec = 15f;  // หักพลังงานต่อวินาทีเมื่อเปิดโหมด
    [SerializeField] private float energyGainPerHit = 10f;  // ได้พลังงานเมื่อฟันโดนศัตรู
    [SerializeField] private int flameDamageBonus = 15;
    [SerializeField] private float flameAttackRateMultiplier = 1.5f; // ตีเร็วขึ้น 50%

    [Header("Pure Flame Mode - Overheat System")]
    [SerializeField] private bool isOverheated = false;
    [SerializeField] private float overheatDuration = 3f;  // ติด Overheat 3 วินาที
    [SerializeField] private int overheatSelfDamage = 5;    // เลือดลดเมื่อเกิด Overheat

    [Header("Pure Flame Skill - Flame Burst (Key: R)")]
    [SerializeField] private float flameSkillCost = 25f;
    [SerializeField] private float flameBurstRadius = 3f;
    [SerializeField] private int flameBurstDamage = 40;
    [SerializeField] private LayerMask enemyLayer;

    private PlayerController2D controller;
    private float nextAttackTime = 0f;
    private int comboStep = 0;

    // Properties
    public bool IsFlameInfused => isFlameInfused;
    public bool IsOverheated => isOverheated;
    public float CurrentFlameEnergy => currentFlameEnergy;
    public float MaxFlameEnergy => maxFlameEnergy;
    public int CurrentDamage => baseDamage + (isFlameInfused ? flameDamageBonus : 0);
    public float CurrentAttackRate => baseAttackRate * (isFlameInfused ? flameAttackRateMultiplier : 1f);

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        DisableAllHitboxes();
    }

    private void Update()
    {
        HandleFlameEnergyLogic();

        // Toggle Pure Flame Mode (กด F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlameInfusion();
        }

        // Active Skill: Flame Burst (กด R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            UseFlameBurstSkill();
        }

        // Basic Attack Logic
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J))
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / CurrentAttackRate);
            }
        }
    }

    private void HandleFlameEnergyLogic()
    {

        if (isFlameInfused)
        {
            // หักเกจไฟตามเวลาขณะเปิดโหมด
            currentFlameEnergy -= energyCostPerSec * Time.deltaTime;

            // เมื่อเกจไฟหมด -> ติดสถานะ Overheat
            if (currentFlameEnergy <= 0f)
            {
                currentFlameEnergy = 0f;
                StartCoroutine(TriggerOverheatRoutine());
            }
        }
        else if (!isOverheated)
        {
            // เกจค่อยๆ ฟื้นฟูขึ้นเองเรื่อยๆ ถ้าไม่ติด Overheat
            if (currentFlameEnergy < maxFlameEnergy)
            {
                currentFlameEnergy += passiveRegenRate * Time.deltaTime;
                currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);
            }
        }
    }

    private void ToggleFlameInfusion()
    {
        if (isOverheated)
        {
            Debug.Log("<color=red>[Flame Mode] ไม่สามารถเปิดใช้งานได้! กำลังติดสถานะ Overheat!</color>");
            return;
        }

        if (!isFlameInfused)
        {
            if (currentFlameEnergy >= minEnergyToActivate)
            {
                isFlameInfused = true;
                Debug.Log("<color=orange>[Flame Mode] เปิดใช้งาน Pure Flame Mode!</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>[Flame Mode] พลังงานไม่พอ! ต้องการอย่างน้อย {minEnergyToActivate}</color>");
            }
        }
        else
        {
            isFlameInfused = false;
            Debug.Log("<color=white>[Flame Mode] ปิดใช้งาน Pure Flame Mode</color>");
        }
    }

    private IEnumerator TriggerOverheatRoutine()
    {
        isFlameInfused = false;
        isOverheated = true;

        Debug.Log($"<color=red>[OVERHEAT!] ความร้อนเกินขีดจำกัด! โดนความเสียหาย {overheatSelfDamage} ดาเมจ และไม่สามารถใช้ไฟได้ {overheatDuration} วินาที!</color>");

        // เรียกใช้ TakeDamage ของผู้เล่นตรงนี้ได้ (ถ้ามี HealthSystem)
        // GetComponent<PlayerHealth>()?.TakeDamage(overheatSelfDamage);

        yield return new WaitForSeconds(overheatDuration);

        isOverheated = false;
        Debug.Log("<color=green>[OVERHEAT END] ระบบระบายความร้อนเสร็จสิ้น เริ่มฟื้นฟูพลังงานไฟ</color>");
    }

    private void UseFlameBurstSkill()
    {
        if (currentFlameEnergy < flameSkillCost)
        {
            Debug.Log("<color=yellow>[Skill] พลังงานไฟไม่พอสำหรับปล่อย Flame Burst!</color>");
            return;
        }

        currentFlameEnergy -= flameSkillCost;
        Debug.Log("<color=orange>[SKILL] ปล่อยคลื่นความร้อน FLAME BURST!</color>");

        // ตรวจจับศัตรูรอบตัวในรัศมี
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, flameBurstRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"<color=red>[Flame Burst Hit] ศัตรู {enemy.name} โดนระเบิดไฟ {flameBurstDamage} ดาเมจ!</color>");
            // enemy.GetComponent<EnemyHealth>()?.TakeDamage(flameBurstDamage);
        }
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        DisableAllHitboxes();

        if (verticalInput > 0)
        {
            Debug.Log("<color=cyan>[Combat] โจมตีทิศทาง: บน (UP ATTACK)</color>");
            StartCoroutine(ActivateHitboxRoutine(upHitbox));
        }
        else if (verticalInput < 0 && !GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            Debug.Log("<color=yellow>[Combat] โจมตีทิศทาง: ล่าง (DOWN ATTACK / POGO)</color>");
            StartCoroutine(ActivateHitboxRoutine(downHitbox));
        }
        else
        {
            comboStep = (comboStep % 3) + 1;
            Debug.Log($"<color=green>[Combat] โจมตีทิศทาง: หน้า (FORWARD ATTACK - Combo {comboStep})</color>");
            StartCoroutine(ActivateHitboxRoutine(forwardHitbox));
        }
    }

    private IEnumerator ActivateHitboxRoutine(GameObject targetHitbox)
    {
        if (targetHitbox == null) yield break;

        targetHitbox.SetActive(true);
        yield return new WaitForSeconds(hitboxActiveTime);
        targetHitbox.SetActive(false);
    }

    private void DisableAllHitboxes()
    {
        if (forwardHitbox) forwardHitbox.SetActive(false);
        if (upHitbox) upHitbox.SetActive(false);
        if (downHitbox) downHitbox.SetActive(false);
    }

    // ฟังก์ชันรับพลังงานเพิ่มเมื่อโจมตีโดนศัตรูจริง
    public void AddEnergyOnHit()
    {
        if (!isOverheated && !isFlameInfused)
        {
            currentFlameEnergy += energyGainPerHit;
            currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);
            Debug.Log($"<color=orange>[Energy Gain] ได้รับพลังงานไฟ +{energyGainPerHit} (ปัจจุบัน: {currentFlameEnergy:F1})</color>");
        }
    }

    public void TriggerPogoBounce()
    {
        controller.Bounce(pogoBounceForce);
    }

    private void OnDrawGizmosSelected()
    {
        // แสดงรัศมีสกิล Flame Burst
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, flameBurstRadius);
    }
}