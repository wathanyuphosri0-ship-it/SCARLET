using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCombatSystem combatSystem;
    [SerializeField] private bool isDownHitbox = false;
    [SerializeField] private LayerMask enemyLayer;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจสอบว่าวัตถุที่ชนอยู่ใน Layer ศัตรูหรือไม่
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            int damage = combatSystem.CurrentDamage;
            Debug.Log($"<color=red>[Hit Success] โจมตีโดน {other.name} ดาเมจ: {damage}</color>");

            if (isDownHitbox)
            {
                combatSystem.TriggerPogoBounce();
            }

            // other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }
    }

    // วาดขอบเขต Hitbox สีแดงสว่างในหน้า Scene เมื่อ Hitbox ถูกเปิดใช้งาน
    private void OnDrawGizmos()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // สีแดงโปร่งแสง

        if (myCol is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (myCol is CircleCollider2D circle)
        {
            Gizmos.DrawSphere((Vector2)transform.position + circle.offset, circle.radius);
        }
    }
}