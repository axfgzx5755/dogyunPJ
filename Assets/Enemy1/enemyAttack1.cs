using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAttack1 : MonoBehaviour
{
    public string playerTag = "Player";
    private Transform target;

    public float minFireDistance = 6f;          // �� �Ÿ� �̻��� �� ���
    public float maxFireDistance = 40f;         // �� �Ÿ� ���̸� ��� ����
    public float fireCooldown = 1.25f;

    public Transform firePoint;                 // �ѱ�/�߻� ��ġ(������ transform ���)
    public GameObject projectilePrefab;         // �ݵ�� Rigidbody ����
    public float projectileSpeed = 22f;         // �ʱ� �ӵ� (m/s)
    public float projectileLifetime = 6f;       // �ڵ� �ı� �ð�(��)

    public bool faceToTargetWhenFire = true;    // �߻� ���� Ÿ���� �ٶ󺸰� ȸ��

    private float nextFireTime = 0f;            // ���� ���ݱ����� ��Ÿ��

    private void Start()
    {
        AcquireTarget();
            if (firePoint == null) firePoint = transform;
    }

    private void Update()
    {
        if (target == null) 
        { AcquireTarget(); return; }

        float dist = Vector3.Distance(transform.position, target.position);
            if (Time.time < nextFireTime) return;
            if (dist < minFireDistance || dist > maxFireDistance) return;

        Fire();
            nextFireTime = Time.time + fireCooldown;
    }

    private void AcquireTarget()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) target = p.transform;
    }

    private void Fire()
    {
        if (projectilePrefab == null) { Debug.LogWarning("[EnemyRangedAttack] projectilePrefab�� ����ֽ��ϴ�."); return; }

        // 3D ����(�밢 ����) ���: �߻� ���� -> Ÿ�� ����
        Vector3 from = firePoint.position;
        Vector3 to = target.position;
        Vector3 dir = (to - from).normalized; // X/Y/Z ��ü ���

        if (faceToTargetWhenFire && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = look;
        }

        GameObject proj = Instantiate(projectilePrefab, from, Quaternion.LookRotation(dir, Vector3.up));

        // Rigidbody�� �ʱ� �ӵ� �ο� (�߷� ��뿩�δ� �������� Rigidbody ������ ����)
        Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {   rb.velocity = dir * projectileSpeed;    }      // �� ���� ���� �밢�� �̵�
            else
            {Debug.LogWarning("[EnemyRangedAttack] projectilePrefab�� Rigidbody�� �����ϴ�. Rigidbody�� �߰��ϼ���.");}

        if (projectileLifetime > 0f) Destroy(proj, projectileLifetime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, minFireDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, maxFireDistance);
            if (firePoint != null)
            {
                Gizmos.color = Color.white;
                    Gizmos.DrawLine(transform.position, firePoint.position);
            }
    }
#endif
}

