using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAttack2 : MonoBehaviour
{
    public string playerTag = "Player";
    private Transform target;

    public float attackRange = 2.2f;                // �� �Ÿ� ���ϸ� ����
    public float attackCooldown = 0.9f;
    private float nextAttackTime = 0f;

    public Transform hitOrigin;                     // Ÿ�� ����(������ transform)
    public float hitRadius = 1.5f;                  // ��ü ����
    public LayerMask playerLayer;                   // �÷��̾� ���� ���̾�

    public Animator animator;
    public string attackTriggerName = "Attack2";    // ���2 Ʈ���Ÿ�(�ʿ�� ����)

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

        // ������ ����: OverlapSphere�� �÷��̾�� ��Ʈ
        Collider[] hits = Physics.OverlapSphere(hitOrigin.position, hitRadius, playerLayer, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                    // ���⿡ ���� ������ ���� ���� (��: PlayerHealth)
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

