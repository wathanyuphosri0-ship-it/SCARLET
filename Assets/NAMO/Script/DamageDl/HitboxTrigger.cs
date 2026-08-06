using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCombatSystem combatSystem;
    [SerializeField] private bool isDownHitbox = false;
    [SerializeField] private LayerMask enemyLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            int damage = combatSystem.CurrentDamage;
            Debug.Log($"<color=red>[Hit Success] โจมตีโดน {other.name} ดาเมจ: {damage}</color>");

            // เติมพลังงานไฟเพิ่มเมื่อฟันโดนศัตรู
            combatSystem.AddEnergyOnHit();

            if (isDownHitbox)
            {
                combatSystem.TriggerPogoBounce();
            }

            // other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);

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