using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using KDGame.Combat; // �� ���� ���ӽ����̽��� �����Ͽ� IDamageable �ν�

[DisallowMultipleComponent]
public class enemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Min(1)] public int maxHealth = 100;
    [Min(0)] public int currentHealth = 100;
    public bool startFullOnAwake = true;
    public bool printDamageLog = false;

    [Header("Hit Reaction")]
    public Animator animator;              // ������ �ڽĿ��� �ڵ� Ž��
    public string hitTriggerName = "Hit";  // �ǰ� Ʈ����
    public float stunDuration = 0.25f;     // ����(��)
    public float knockbackForce = 0f;      // �˹�(0�̸� �̻��)
    public ForceMode knockbackForceMode = ForceMode.Impulse;

    [Header("Death")]
    public string deathTriggerName = "Die"; // ��� Ʈ����
    public float disableDelay = 0.05f;      // ��� ��Ȱ��ȭ ����
    public float destroyDelay = 3.0f;       // �ı� ����(<=0�̸� �ı� �� ��)
    public bool disableCollidersOnDeath = true;

    [Header("Behaviours (�ϰ� On/Off)")]
    public enemyFollow follow;     // �̵� ��ũ��Ʈ(�ܺ�)  
    public enemyAttack1 attack1;   // ���� ��ũ��Ʈ(�ܺ�)  
    public Behaviour[] extraBehaviours;

    [Header("Physics/Colliders")]
    public Rigidbody rb;           // ����: ����/�˹�/����
    public Collider[] colliders;   // ���� �ڵ� ����

    [Header("Events")]
    public UnityEvent onDamaged;   // �ǰ� ��
    public UnityEvent onDied;      // ��� ����

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

    // ===== ��� �ϰ� ���� =====
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

    // ===== IDamageable ���� =====
    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator)
    {
        if (isDead || amount <= 0) return;

        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (printDamageLog)
            Debug.Log($"[EnemyCore] {name} took {amount} �� {currentHealth}/{maxHealth}");

        onDamaged?.Invoke();

        // �ǰ� ����
        if (!isDead)
        {
            if (animator && !string.IsNullOrEmpty(hitTriggerName))
                animator.SetTrigger(hitTriggerName);

            // ����
            if (stunDuration > 0f) StartCoroutine(Co_Stun(stunDuration));

            // �˹�
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

        // ���� ����
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // �̵�/���� �� ��� ��Ȱ��ȭ
        if (disableDelay > 0f) Invoke(nameof(_DisableModules), disableDelay);
        else _DisableModules();

        // �ݶ��̴� ��Ȱ��ȭ
        if (disableCollidersOnDeath)
        {
            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(includeInactive: false);

            foreach (var c in colliders)
                if (c != null) c.enabled = false;
        }

        // �ı� ����
        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
    }

    private void _DisableModules() => DisableAll();

    // ����� ����
    [ContextMenu("Test / Damage 10")] private void _TestDamage10() => TakeDamage(10, transform.position, Vector3.up, null);
    [ContextMenu("Test / Kill")] private void _TestKill() => Die();
}