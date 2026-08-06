using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hitbox GameObjects")]
    [SerializeField] private GameObject sideHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;
    [SerializeField] private GameObject flameBurstHitbox; // Hitbox ระเบิดพลังไฟรอบตัว
    [SerializeField] private float hitboxActiveTime = 0.15f;

    [Header("Attack Settings")]
    [SerializeField] private int baseAttackDamage = 1;
    [SerializeField] private float attackRate = 3.5f;

    [Header("3-Hit Combo Settings")]
    [SerializeField] private float comboResetTime = 0.8f;
    private int currentComboStep = 1;
    private float lastAttackTime = 0f;

    [Header("Pure Flame Gauge Settings")]
    [SerializeField] private float maxFlameEnergy = 100f;
    [SerializeField] private float currentFlameEnergy = 100f; // เริ่มต้น 100 สำหรับทดสอบ
    [SerializeField] private float flameDrainRate = 5f; // ลด 5 ต่อ 1 วินาที
    [SerializeField] private KeyCode pureFlameKey = KeyCode.F;
    [SerializeField] private KeyCode flameSkillKey = KeyCode.E; // ปุ่มใช้สกิล (กด E)
    [SerializeField] private float skillEnergyCost = 30f; // ใช้ 30 เกจ

    [Header("Pure Flame Buffs")]
    [SerializeField] private float flameModeDamageMultiplier = 1.8f; // ตีแรงขึ้น x1.8
    [SerializeField] private float moveSpeedBuffMultiplier = 1.35f;  // เดินเร็วขึ้น x1.35

    private bool isPureFlameMode = false;
    private float nextAttackTime = 0f;
    private PlayerController2D playerController;
    private Rigidbody2D rb;

    public float CurrentFlameEnergy => currentFlameEnergy;
    public float MaxFlameEnergy => maxFlameEnergy;
    public int CurrentDamage => Mathf.RoundToInt(baseAttackDamage * (isPureFlameMode ? flameModeDamageMultiplier : 1f));
    public bool IsPureFlameMode => isPureFlameMode;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        rb = GetComponent<Rigidbody2D>();
        DisableAllHitboxes();
    }

    private void Update()
    {
        // 1. สลับเปิด/ปิด Pure Flame Mode (ปุ่ม F)
        if (Input.GetKeyDown(pureFlameKey))
        {
            if (!isPureFlameMode && currentFlameEnergy > 0 && !playerController.IsOverheated)
            {
                ActivatePureFlameMode();
            }
            else if (isPureFlameMode)
            {
                DeactivatePureFlameMode(false); // ปิดเองโดยผู้เล่น
            }
        }

        // 2. ระบบลดเกจไฟทีละ 5 ต่อวินาที (ขณะเปิดโหมด)
        if (isPureFlameMode)
        {
            currentFlameEnergy -= flameDrainRate * Time.deltaTime;
            currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);

            // หากปล่อยให้เกจหมดเอง -> เกิด Overheat
            if (currentFlameEnergy <= 0f)
            {
                DeactivatePureFlameMode(true); // ปล่อยหมดเอง = Overheat!
            }

            // 3. ปุ่มกดใช้สกิลระเบิดไฟรอบตัว (ปุ่ม E)
            if (Input.GetKeyDown(flameSkillKey))
            {
                UseFlameBurstSkill();
            }
        }

        // 4. เช็ครีเซ็ต Combo
        if (Time.time - lastAttackTime > comboResetTime && currentComboStep != 1)
        {
            currentComboStep = 1;
            Debug.Log("<color=grey>[COMBO RESET] หมดเวลาคอมโบ! รีเซ็ตกลับเป็น Combo 1</color>");
        }

        // 5. การโจมตีปกติ
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J))
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
    }

    private void ActivatePureFlameMode()
    {
        isPureFlameMode = true;
        playerController.ApplySpeedBuff(moveSpeedBuffMultiplier);
        Debug.Log($"<color=orange><b>[PURE FLAME MODE] 💥 เปิดใช้งานโหมดไฟบริสุทธิ์! (Speed Buff x{moveSpeedBuffMultiplier} | Damage x{flameModeDamageMultiplier}) เกจคงเหลือ: {currentFlameEnergy:F1}/{maxFlameEnergy}</b></color>");
    }

    private void DeactivatePureFlameMode(bool isOverheated)
    {
        isPureFlameMode = false;
        playerController.RemoveSpeedBuff();

        if (isOverheated)
        {
            Debug.Log("<color=red><b>[OVERHEAT!] ⚠️ เกจไฟหมดถัง! เกิดอาการ Overheat เคลื่อนที่ช้าลง 2 วินาที!</b></color>");
            playerController.ApplyOverheatPenalty(2f, 0.5f); // ช้าลง 50% เป็นเวลา 2 วินาที
        }
        else
        {
            Debug.Log("<color=cyan>[PURE FLAME MODE] ❄️ ปิดใช้งานโหมดไฟบริสุทธิ์ กลับสู่สถานะปกติ</color>");
        }
    }

    private void UseFlameBurstSkill()
    {
        if (currentFlameEnergy >= skillEnergyCost)
        {
            currentFlameEnergy -= skillEnergyCost;
            Debug.Log($"<color=red><b>[SKILL] 💥 Flame Burst! ระเบิดพลังไฟรอบตัว! (ใช้เกจ {skillEnergyCost} | เกจคงเหลือ: {currentFlameEnergy:F1}/{maxFlameEnergy})</b></color>");

            if (flameBurstHitbox != null)
            {
                StartCoroutine(ActivateHitboxRoutine(flameBurstHitbox));
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[SKILL FAILED] ❌ เกจไฟไม่พอใช้สกิล! (ต้องการ {skillEnergyCost} | มีแค่ {currentFlameEnergy:F1})</color>");
        }
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        DisableAllHitboxes();
        lastAttackTime = Time.time;

        if (verticalInput > 0.1f && upHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👆 โจมตีทิศทาง: UP (ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(upHitbox));
        }
        else if (verticalInput < -0.1f && !playerController.IsGrounded() && downHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👇 โจมตีทิศทาง: DOWN / POGO (ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(downHitbox));
        }
        else if (sideHitbox != null)
        {
            Debug.Log($"<color=red>[ATTACK] ⚔️ โจมตีทิศทาง: SIDE (Combo Step: {currentComboStep}/3 | ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(sideHitbox));
            currentComboStep = (currentComboStep % 3) + 1;
        }
    }

    private IEnumerator ActivateHitboxRoutine(GameObject targetHitbox)
    {
        targetHitbox.SetActive(true);
        yield return new WaitForSeconds(hitboxActiveTime);
        targetHitbox.SetActive(false);
    }

    private void DisableAllHitboxes()
    {
        if (sideHitbox != null) sideHitbox.SetActive(false);
        if (upHitbox != null) upHitbox.SetActive(false);
        if (downHitbox != null) downHitbox.SetActive(false);
        if (flameBurstHitbox != null) flameBurstHitbox.SetActive(false);
    }

    // ฟังก์ชันสำหรับเติมเกจไฟเมื่อฟันโดนศัตรู
    public void AddFlameEnergy(float amount)
    {
        currentFlameEnergy = Mathf.Clamp(currentFlameEnergy + amount, 0f, maxFlameEnergy);
        Debug.Log($"<color=orange>[ENERGY RECOVER] +{amount} Flame Energy (ปัจจุบัน: {currentFlameEnergy:F1}/{maxFlameEnergy})</color>");
    }
}