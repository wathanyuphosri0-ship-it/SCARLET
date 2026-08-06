using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private bool isDownHitbox = false;
    [SerializeField] private LayerMask enemyLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจสอบว่า GameObject อยู่ใน Layer ที่กำหนดไว้หรือไม่
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            // ตรวจหา BaseEnemy เพื่อทำดาเมจ
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(1, transform.position); // ทำดาเมจใส่ศัตรู
                Debug.Log($"<color=red>[Hit Success] โจมตีโดน {other.name}</color>");

                // ถ้าเป็น Hitbox ด้านล่าง ให้เรียกระบบ Pogo Bounce (เด้งตัว)
                if (isDownHitbox)
                {
                    PlayerController2D playerCtrl = combat != null ? combat.GetComponent<PlayerController2D>() : GetComponentInParent<PlayerController2D>();
                    if (playerCtrl != null)
                    {
                        playerCtrl.Bounce(14f); // แรงเด้งขึ้นฟ้า
                    }
                }
            }
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
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(circle.offset, circle.radius);
        }
    }
}