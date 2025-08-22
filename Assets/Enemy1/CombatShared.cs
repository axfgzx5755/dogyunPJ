using UnityEngine;

namespace KDGame.Combat
{
    [DisallowMultipleComponent]
    [AddComponentMenu("KDGame/Internal/Combat Shared (Container)")]

    // 이 컴포넌트는 "파일-클래스 매핑"을 안전하게 해주기 위한 컨테이너
    public class CombatShared : MonoBehaviour
    {
        // 실제 게임플레이 로직은 아래 TeamTag / Projectile 컴포넌트를 사용.
        // 이 컨테이너는 동작 안함.
    }

    
    public interface IDamageable        // ===== 공용 인터페이스 / 팀 태그 =====
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
    public class Projectile : MonoBehaviour     // ===== 투사체 =====
    {
        [Header("Spec")]
        public int damage = 10;
        public float lifeTime = 6f;             // 자동 파괴 시간
        public bool useTrigger = true;          // 콜라이더 Trigger 사용
        public bool friendlyFire = false;       // 같은 팀도 피격 허용
        public int maxPenetrations = 0;         // 0=관통없음, N회 관통

        [Header("Team / Filtering")]
        public Team team = Team.Enemy;          // 투사체 소유 팀
        public LayerMask hitMask = ~0;          // 피격 허용 레이어

        [Header("Owner Ignore")]
        public GameObject owner;                // 발사자(자기 자신 무시)
        public float ignoreOwnerTime = 0.05f;
        private float bornTime;

        [Header("Effects (선택)")]
        public GameObject hitEffectPrefab;      // 피격 이펙트
        public bool stickOnHit = false;         // 표면에 붙이기(관통 X일 때)
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

        /// 발사 직후 오너/팀 세팅(선택)
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
            // 발사 직후, 자기 자신/자식 무시
            if (owner != null && (other == owner || other.transform.IsChildOf(owner.transform)))
                if (Time.time - bornTime <= ignoreOwnerTime) return;

            // 레이어 필터
            if (((1 << other.layer) & hitMask) == 0) return;

            // 팀 필터
            if (!friendlyFire && IsSameTeam(other)) return;

            // 대미지 적용
            Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : col.ClosestPoint(transform.position);
            Vector3 hitNormal = collision.contactCount > 0 ? collision.contacts[0].normal : (-transform.forward);
            bool applied = TryApplyDamage(other, damage, hitPoint, hitNormal);

            // 이펙트
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));

            // 관통 처리
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

            // 폴백: Unity Tag
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
