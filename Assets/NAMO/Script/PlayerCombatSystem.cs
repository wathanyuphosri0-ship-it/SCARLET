using System.Collections;
using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Hitbox Colliders")]
    [SerializeField] private Collider2D forwardHitbox;
    [SerializeField] private Collider2D upHitbox;
    [SerializeField] private Collider2D downHitbox;
    [SerializeField] private float hitboxActiveTime = 0.1f; // ระยะเวลาที่เปิด Hitbox ค้างไว้ต่อการฟัน 1 ครั้ง

    [Header("Attack Settings")]
    [SerializeField] private float attackRate = 3.5f;

    [Header("Damage Settings")]
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private int flameDamageBonus = 10;
    [SerializeField] private float pogoBounceForce = 14f;

    [Header("Pure Flame Mode")]
    [SerializeField] private bool isFlameInfused = false;
    [SerializeField] private float flameEnergyCostPerSec = 5f;
    [SerializeField] private float currentFlameEnergy = 100f;

    private PlayerController2D controller;
    private float nextAttackTime = 0f;
    private int comboStep = 0;

    public int CurrentDamage => baseDamage + (isFlameInfused ? flameDamageBonus : 0);

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();

        // ปิด Hitbox ทุกตัวไว้ก่อนเริ่มเกม
        DisableAllHitboxes();
    }

    private void Update()
    {
        // Toggle Pure Flame Mode
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

        // Attack Input
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

    private IEnumerator ActivateHitboxRoutine(Collider2D targetHitbox)
    {
        if (targetHitbox == null) yield break;

        // เปิดใช้งาน Hitbox
        targetHitbox.enabled = true;

        // รอตามระยะเวลาที่กำหนดให้ Hitbox ทำงาน
        yield return new WaitForSeconds(hitboxActiveTime);

        // ปิดการใช้งาน Hitbox
        targetHitbox.enabled = false;
    }

    private void DisableAllHitboxes()
    {
        if (forwardHitbox) forwardHitbox.enabled = false;
        if (upHitbox) upHitbox.enabled = false;
        if (downHitbox) downHitbox.enabled = false;
    }

    public void TriggerPogoBounce()
    {
        controller.Bounce(pogoBounceForce);
    }

    private void ToggleFlameInfusion()
    {
        if (currentFlameEnergy > 10f)
        {
            isFlameInfused = !isFlameInfused;
            Debug.Log($"Pure Flame Infusion State: {isFlameInfused}");
        }
    }
}