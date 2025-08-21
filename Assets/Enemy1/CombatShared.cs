using UnityEngine;

namespace KDGame.Combat
{
    [DisallowMultipleComponent]
    [AddComponentMenu("KDGame/Internal/Combat Shared (Container)")]

    // �� ������Ʈ�� "����-Ŭ���� ����"�� �����ϰ� ���ֱ� ���� �����̳�
    public class CombatShared : MonoBehaviour
    {
        // ���� �����÷��� ������ �Ʒ� TeamTag / Projectile ������Ʈ�� ���.
        // �� �����̳ʴ� ���� ����.
    }

    
    public interface IDamageable        // ===== ���� �������̽� / �� �±� =====
    {
        void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator);
    }

    public enum Team
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    [AddComponentMenu("KDGame/Team Tag")]
    public class TeamTag : MonoBehaviour
    {
        public Team team = Team.Enemy;
    }

    
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("KDGame/Projectile")]
    public class Projectile : MonoBehaviour     // ===== ����ü =====
    {
        [Header("Spec")]
        public int damage = 10;
        public float lifeTime = 6f;             // �ڵ� �ı� �ð�
        public bool useTrigger = true;          // �ݶ��̴� Trigger ���
        public bool friendlyFire = false;       // ���� ���� �ǰ� ���
        public int maxPenetrations = 0;         // 0=�������, Nȸ ����

        [Header("Team / Filtering")]
        public Team team = Team.Enemy;          // ����ü ���� ��
        public LayerMask hitMask = ~0;          // �ǰ� ��� ���̾�

        [Header("Owner Ignore")]
        public GameObject owner;                // �߻���(�ڱ� �ڽ� ����)
        public float ignoreOwnerTime = 0.05f;
        private float bornTime;

        [Header("Effects (����)")]
        public GameObject hitEffectPrefab;      // �ǰ� ����Ʈ
        public bool stickOnHit = false;         // ǥ�鿡 ���̱�(���� X�� ��)
        public Rigidbody rb;

        private int penetrations;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = useTrigger;

            bornTime = Time.time;
            if (lifeTime > 0f) Destroy(gameObject, lifeTime);
        }

        /// �߻� ���� ����/�� ����(����)
        public void Init(GameObject owner, Team team)
        {
            this.owner = owner;
            this.team = team;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!useTrigger) return;
            HandleHit(other.gameObject, other, default);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (useTrigger) return;
            HandleHit(collision.gameObject, collision.collider, collision);
        }

        private void HandleHit(GameObject other, Collider col, Collision collision)
        {
            // �߻� ����, �ڱ� �ڽ�/�ڽ� ����
            if (owner != null && (other == owner || other.transform.IsChildOf(owner.transform)))
                if (Time.time - bornTime <= ignoreOwnerTime) return;

            // ���̾� ����
            if (((1 << other.layer) & hitMask) == 0) return;

            // �� ����
            if (!friendlyFire && IsSameTeam(other)) return;

            // ����� ����
            Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : col.ClosestPoint(transform.position);
            Vector3 hitNormal = collision.contactCount > 0 ? collision.contacts[0].normal : (-transform.forward);
            bool applied = TryApplyDamage(other, damage, hitPoint, hitNormal);

            // ����Ʈ
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));

            // ���� ó��
            if (maxPenetrations > 0)
            {
                penetrations++;
                if (penetrations > maxPenetrations) Destroy(gameObject);
            }
            else
            {
                if (stickOnHit && applied && col != null)
                    StickTo(col.transform, hitPoint, hitNormal);

                Destroy(gameObject);
            }
        }

        private bool TryApplyDamage(GameObject target, int amount, Vector3 point, Vector3 normal)
        {
            var dmg = target.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(amount, point, normal, owner != null ? owner : gameObject);
                return true;
            }
            return false;
        }

        private bool IsSameTeam(GameObject other)
        {
            var tag = other.GetComponentInParent<TeamTag>();
            if (tag != null) return tag.team == team;

            // ����: Unity Tag
            if (team == Team.Enemy && other.CompareTag("Enemy")) return true;
            if (team == Team.Player && other.CompareTag("Player")) return true;
            return false;
        }

        private void StickTo(Transform parent, Vector3 point, Vector3 normal)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.position = point;
            transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
            transform.SetParent(parent, true);
        }
    }
}
