using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    [Header("Invincibility (i-frames) Settings")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    [SerializeField] private float flashInterval = 0.1f;
    private bool isInvincible = false;

    [Header("Knockback Settings")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(8f, 10f);
    [SerializeField] private float knockbackDuration = 0.25f;
    private bool isKnockedBack = false;

    [Header("Components & References")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController2D playerController;

    // Public Properties สำหรับให้ระบบ UI หรือระบบอื่นมาดึงค่าไปใช้
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        playerController = GetComponent<PlayerController2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// ฟังก์ชันรับความเสียหาย (Take Damage)
    /// </summary>
    /// <param name="damage">จำนวนความเสียหาย</param>
    /// <param name="damageSourcePosition">ตำแหน่งของจุดที่สร้างดาเมจ (เช่น ตำแหน่งศัตรู/กับดัก)</param>
    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        // หากอยู่อินวินซิเบิล (อมตะ) หรือกำลังโดน Knockback จะไม่ได้รับดาเมจซ้ำ
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"<color=red>[PLAYER HIT] เลือดเหลือ: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // คำนวณทิศทาง Knockback (ดันออกจากจุดกำเนิดดาเมจ)
            float knockbackDirection = transform.position.x < damageSourcePosition.x ? -1f : 1f;
            StartCoroutine(ApplyKnockbackRoutine(knockbackDirection));
            StartCoroutine(InvincibilityRoutine());
        }
    }

    /// <summary>
    /// ฟังก์ชันฮีลเลือด
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"<color=green>[HEAL] ฟื้นฟูเลือด: +{amount} (ปัจจุบัน: {currentHealth}/{maxHealth})</color>");
    }

    private IEnumerator ApplyKnockbackRoutine(float directionX)
    {
        isKnockedBack = true;

        // หากมี PlayerController ให้ปิดการควบคุมของผู้เล่นชั่วคราว
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // รีเซ็ตความเร็วเดิมก่อนใส่แรง Knockback
        rb.linearVelocity = Vector2.zero;
        Vector2 force = new Vector2(directionX * knockbackForce.x, knockbackForce.y);
        rb.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        // คืนการควบคุมให้ผู้เล่น
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        isKnockedBack = false;
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0f;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        // กะพริบ Sprite ขณะอยู่ใน i-frames
        while (timer < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = (c.a == 1f) ? 0.3f : 1f; // สลับความโปร่งใส
                spriteRenderer.color = c;
            }

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        // คืนค่า Sprite เป็นปกติเมื่อหมด i-frames
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("<color=black><b>[GAME OVER] ตัวละครเสียชีวิต!</b></color>");

        // ปิดการควบคุม
        if (playerController != null) playerController.enabled = false;

        // สามารถใส่ Event / Animation การตาย หรือสั่ง Reload Scene ได้ที่นี่
    }
}