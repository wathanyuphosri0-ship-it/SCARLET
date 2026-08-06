using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hitbox GameObjects")]
    [SerializeField] private GameObject sideHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;
    [SerializeField] private float hitboxActiveTime = 0.15f;

    [Header("Attack Settings")]
    [SerializeField] private int baseAttackDamage = 1;
    [SerializeField] private float attackRate = 3.5f;

    [Header("3-Hit Combo Settings")]
    [SerializeField] private float comboResetTime = 0.8f; // เวลาที่ถ้านิ่งนานเกินไป Combo จะรีเซ็ตกลับเป็น 1
    private int currentComboStep = 1;
    private float lastAttackTime = 0f;

    [Header("Pure Flame Mode Settings")]
    [SerializeField] private bool isPureFlameMode = false;
    [SerializeField] private float flameModeDamageMultiplier = 1.5f; // ตัวคูณดาเมจโหมดไฟ (x1.5)
    [SerializeField] private KeyCode pureFlameKey = KeyCode.F;

    private float nextAttackTime = 0f;
    private PlayerController2D playerController;
    private Rigidbody2D rb;

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
        // สลับเปิด/ปิด Pure Flame Mode เมื่อกดปุ่ม F
        if (Input.GetKeyDown(pureFlameKey))
        {
            TogglePureFlameMode();
        }

        // เช็ครีเซ็ต Combo หากไม่ได้ฟันต่อเนื่องภายในระยะเวลาที่กำหนด
        if (Time.time - lastAttackTime > comboResetTime && currentComboStep != 1)
        {
            currentComboStep = 1;
            Debug.Log("<color=grey>[COMBO RESET] หมดเวลาคอมโบ! รีเซ็ตกลับเป็น Combo 1</color>");
        }

        // การโจมตี
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J))
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
    }

    private void TogglePureFlameMode()
    {
        isPureFlameMode = !isPureFlameMode;

        if (isPureFlameMode)
        {
            Debug.Log("<color=orange><b>[PURE FLAME MODE] 💥 เปิดใช้งานโหมดไฟบริสุทธิ์! (Damage Buff Active)</b></color>");
        }
        else
        {
            Debug.Log("<color=cyan>[PURE FLAME MODE] ❄️ ปิดใช้งานโหมดไฟบริสุทธิ์ กลับสู่สถานะปกติ</color>");
        }
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        DisableAllHitboxes();
        lastAttackTime = Time.time;

        // 1. ฟันขึ้นด้านบน (กด Up + Attack)
        if (verticalInput > 0.1f && upHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👆 โจมตีทิศทาง: UP (ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(upHitbox));
        }
        // 2. ฟันลงด้านล่าง (กด Down + Attack + อยู่กลางอากาศ)
        else if (verticalInput < -0.1f && !playerController.IsGrounded() && downHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👇 โจมตีทิศทาง: DOWN / POGO (ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(downHitbox));
        }
        // 3. ฟันด้านหน้าพร้อมระบบ 3-Hit Combo (Default Side Attack)
        else if (sideHitbox != null)
        {
            Debug.Log($"<color=red>[ATTACK] ⚔️ โจมตีทิศทาง: SIDE (Combo Step: {currentComboStep}/3 | ดาเมจ: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(sideHitbox));

            // ขยับ Combo Step ไปขั้นถัดไป (1 -> 2 -> 3 -> 1)
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
    }
}