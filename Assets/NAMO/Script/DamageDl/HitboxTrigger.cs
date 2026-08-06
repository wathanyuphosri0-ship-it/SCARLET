using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private bool isDownHitbox = false;
    [SerializeField] private LayerMask enemyLayer;

    private void Awake()
    {
        if (combat == null)
        {
            combat = GetComponentInParent<PlayerCombat>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                int damage = combat != null ? combat.CurrentDamage : 1;
                enemy.TakeDamage(damage, transform.position);

                if (combat != null)
                {
                    combat.AddFlameEnergyOnHit();
                }

                Debug.Log($"<color=red>[Hit Success] โจมตีโดน {other.name} (Damage: {damage})</color>");

                if (isDownHitbox)
                {
                    PlayerController2D playerCtrl = combat != null ? combat.GetComponent<PlayerController2D>() : GetComponentInParent<PlayerController2D>();
                    if (playerCtrl != null)
                    {
                        playerCtrl.Bounce(14f);
                    }
                }
            }
        }
        CameraController2D.Instance?.TriggerShake(0.1f, 0.2f);
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
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(circle.offset, circle.radius);
        }
    }
    
}