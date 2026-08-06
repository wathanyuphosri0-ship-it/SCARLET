using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hitbox GameObjects")]
    [SerializeField] private GameObject sideHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;
    [SerializeField] private GameObject flameBurstHitbox;
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
    [SerializeField] private float currentFlameEnergy = 100f;
    [SerializeField] private float flameDrainRate = 5f;
    [SerializeField] private float flameRegenRate = 8f;
    [SerializeField] private float energyGainOnHit = 10f;
    [SerializeField] private KeyCode pureFlameKey = KeyCode.F;
    [SerializeField] private KeyCode flameSkillKey = KeyCode.E;
    [SerializeField] private float skillEnergyCost = 30f;

    [Header("Pure Flame Buffs")]
    [SerializeField] private float flameModeDamageMultiplier = 1.8f;
    [SerializeField] private float moveSpeedBuffMultiplier = 1.35f;

    private bool isFlameActive = false;
    private float nextAttackTime = 0f;
    private PlayerController2D playerController;
    private Rigidbody2D rb;

    public float CurrentFlameEnergy => currentFlameEnergy;
    public float MaxFlameEnergy => maxFlameEnergy;
    public int CurrentDamage => Mathf.RoundToInt(baseAttackDamage * (isFlameActive ? flameModeDamageMultiplier : 1f));
    public bool IsPureFlameMode => isFlameActive;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        rb = GetComponent<Rigidbody2D>();
        DisableAllHitboxes();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pureFlameKey))
        {
            if (!isFlameActive && currentFlameEnergy > 0 && !playerController.IsOverheated)
            {
                ActivatePureFlameMode();
            }
            else if (isFlameActive)
            {
                DeactivatePureFlameMode(false);
            }
        }

        if (isFlameActive)
        {
            currentFlameEnergy -= flameDrainRate * Time.deltaTime;
            currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);

            if (currentFlameEnergy <= 0f)
            {
                DeactivatePureFlameMode(true);
            }

            if (Input.GetKeyDown(flameSkillKey))
            {
                UseFlameBurstSkill();
            }
        }
        else
        {
            if (currentFlameEnergy < maxFlameEnergy)
            {
                currentFlameEnergy += flameRegenRate * Time.deltaTime;
                currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);
            }
        }

        if (Time.time - lastAttackTime > comboResetTime && currentComboStep != 1)
        {
            currentComboStep = 1;
            Debug.Log("<color=grey>[COMBO RESET] หมดเวลาคอมโบ!</color>");
        }

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
        isFlameActive = true;
        playerController.ApplySpeedBuff(moveSpeedBuffMultiplier);
        Debug.Log($"<color=orange>[PURE FLAME] 💥 เปิดโหมดไฟ! ({currentFlameEnergy:F1}/{maxFlameEnergy})</color>");
    }

    private void DeactivatePureFlameMode(bool isOverheated)
    {
        isFlameActive = false;
        playerController.RemoveSpeedBuff();

        if (isOverheated)
        {
            Debug.Log("<color=red>[OVERHEAT!] ⚠️ เกจหมด ติด Overheat 2 วินาที!</color>");
            playerController.ApplyOverheatPenalty(2f, 0.5f);
        }
        else
        {
            Debug.Log("<color=cyan>[PURE FLAME] ❄️ ปิดโหมดไฟ</color>");
        }
    }

    private void UseFlameBurstSkill()
    {
        if (currentFlameEnergy >= skillEnergyCost)
        {
            currentFlameEnergy -= skillEnergyCost;
            Debug.Log($"<color=red>[SKILL] 💥 Flame Burst! (เหลือ: {currentFlameEnergy:F1}/{maxFlameEnergy})</color>");

            if (flameBurstHitbox != null)
            {
                StartCoroutine(ActivateHitboxRoutine(flameBurstHitbox));
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[SKILL FAILED] ❌ เกจไม่พอ!</color>");
        }
        CameraController2D.Instance?.TriggerShake(0.25f, 0.5f);
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        DisableAllHitboxes();
        lastAttackTime = Time.time;

        if (verticalInput > 0.1f && upHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👆 UP (DMG: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(upHitbox));
        }
        else if (verticalInput < -0.1f && !playerController.IsGrounded() && downHitbox != null)
        {
            Debug.Log($"<color=yellow>[ATTACK] 👇 DOWN (DMG: {CurrentDamage})</color>");
            StartCoroutine(ActivateHitboxRoutine(downHitbox));
        }
        else if (sideHitbox != null)
        {
            Debug.Log($"<color=red>[ATTACK] ⚔️ SIDE (Combo: {currentComboStep}/3 | DMG: {CurrentDamage})</color>");
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

    public void AddFlameEnergyOnHit()
    {
        currentFlameEnergy = Mathf.Clamp(currentFlameEnergy + energyGainOnHit, 0f, maxFlameEnergy);
        Debug.Log($"<color=orange>[HIT RECOVER] +{energyGainOnHit} Energy ({currentFlameEnergy:F1}/{maxFlameEnergy})</color>");
    }
}