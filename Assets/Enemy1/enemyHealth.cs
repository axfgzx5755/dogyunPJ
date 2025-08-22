using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using KDGame.Combat; // ← 같은 네임스페이스를 참조하여 IDamageable 인식

[DisallowMultipleComponent]
public class enemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Min(1)] public int maxHealth = 100;
    [Min(0)] public int currentHealth = 100;
    public bool startFullOnAwake = true;
    public bool printDamageLog = false;

    [Header("Hit Reaction")]
    public Animator animator;              // 없으면 자식에서 자동 탐색
    public string hitTriggerName = "Hit";  // 피격 트리거
    public float stunDuration = 0.25f;     // 경직(초)
    public float knockbackForce = 0f;      // 넉백(0이면 미사용)
    public ForceMode knockbackForceMode = ForceMode.Impulse;

    [Header("Death")]
    public string deathTriggerName = "Die"; // 사망 트리거
    public float disableDelay = 0.05f;      // 모듈 비활성화 지연
    public float destroyDelay = 3.0f;       // 파괴 지연(<=0이면 파괴 안 함)
    public bool disableCollidersOnDeath = true;

    [Header("Behaviours (일괄 On/Off)")]
    public enemyFollow follow;     // 이동 스크립트(외부)  
    public enemyAttack1 attack1;   // 공격 스크립트(외부)  
    public Behaviour[] extraBehaviours;

    [Header("Physics/Colliders")]
    public Rigidbody rb;           // 선택: 경직/넉백/정지
    public Collider[] colliders;   // 비우면 자동 수집

    [Header("Events")]
    public UnityEvent onDamaged;   // 피격 시
    public UnityEvent onDied;      // 사망 직후

    private bool isDead = false;

    private void Awake()
    {
        if (startFullOnAwake) currentHealth = maxHealth;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    // ===== 모듈 일괄 제어 =====
    public void SetEnabledAll(bool enabled)
    {
        if (follow != null) follow.enabled = enabled;
        if (attack1 != null) attack1.enabled = enabled;
        if (extraBehaviours != null)
        {
            foreach (var b in extraBehaviours)
                if (b != null) b.enabled = enabled;
        }
    }
    public void DisableAll() => SetEnabledAll(false);
    public void EnableAll() => SetEnabledAll(true);

    // ===== IDamageable 구현 =====
    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator)
    {
        if (isDead || amount <= 0) return;

        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (printDamageLog)
            Debug.Log($"[EnemyCore] {name} took {amount} → {currentHealth}/{maxHealth}");

        onDamaged?.Invoke();

        // 피격 연출
        if (!isDead)
        {
            if (animator && !string.IsNullOrEmpty(hitTriggerName))
                animator.SetTrigger(hitTriggerName);

            // 경직
            if (stunDuration > 0f) StartCoroutine(Co_Stun(stunDuration));

            // 넉백
            if (knockbackForce > 0f && rb != null)
            {
                Vector3 dir = (transform.position - hitPoint).normalized;
                rb.AddForce(dir * knockbackForce, knockbackForceMode);
            }
        }

        if (currentHealth <= 0) Die();
    }

    private IEnumerator Co_Stun(float duration)
    {
        DisableAll();
        yield return new WaitForSeconds(duration);
        if (!isDead) EnableAll();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator && !string.IsNullOrEmpty(deathTriggerName))
            animator.SetTrigger(deathTriggerName);

        onDied?.Invoke();

        // 물리 정지
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 이동/공격 등 기능 비활성화
        if (disableDelay > 0f) Invoke(nameof(_DisableModules), disableDelay);
        else _DisableModules();

        // 콜라이더 비활성화
        if (disableCollidersOnDeath)
        {
            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(includeInactive: false);

            foreach (var c in colliders)
                if (c != null) c.enabled = false;
        }

        // 파괴 예약
        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
    }

    private void _DisableModules() => DisableAll();

    // 디버그 편의
    [ContextMenu("Test / Damage 10")] private void _TestDamage10() => TakeDamage(10, transform.position, Vector3.up, null);
    [ContextMenu("Test / Kill")] private void _TestKill() => Die();
}