using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAttack2 : MonoBehaviour
{
    public string playerTag = "Player";
    private Transform target;

    public float attackRange = 2.2f;                // 이 거리 이하면 공격
    public float attackCooldown = 0.9f;
    private float nextAttackTime = 0f;

    public Transform hitOrigin;                     // 타격 원점(없으면 transform)
    public float hitRadius = 1.5f;                  // 구체 범위
    public LayerMask playerLayer;                   // 플레이어 감지 레이어

    public Animator animator;
    public string attackTriggerName = "Attack2";    // 모션2 트리거명(필요시 변경)

    private void Start()
    {
        AcquireTarget();
            if (hitOrigin == null) hitOrigin = transform;
    }

    private void Update()
    {
        if (target == null)
        { AcquireTarget(); return; }

        float dist = Vector3.Distance(transform.position, target.position);
            if (dist > attackRange) return;
            if (Time.time < nextAttackTime) return;

        DoAttack();
            nextAttackTime = Time.time + attackCooldown;
    }

    private void AcquireTarget()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) target = p.transform;
    }

    private void DoAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        { animator.SetTrigger(attackTriggerName); }

        // 간단한 판정: OverlapSphere로 플레이어에만 히트
        Collider[] hits = Physics.OverlapSphere(hitOrigin.position, hitRadius, playerLayer, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                    // 여기에 실제 데미지 로직 연결 (예: PlayerHealth)
                    // if (h.TryGetComponent<PlayerHealth>(out var hp)) hp.TakeDamage(damage);
                Debug.Log($"[EnemyMeleeAttack] Hit {h.name}");
            }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (hitOrigin != null)
        {
            Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hitOrigin.position, hitRadius);
        }
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}

