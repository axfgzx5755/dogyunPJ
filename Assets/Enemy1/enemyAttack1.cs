using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAttack1 : MonoBehaviour
{
    public string playerTag = "Player";
    private Transform target;

    public float minFireDistance = 6f;          // 이 거리 이상일 때 사격
    public float maxFireDistance = 40f;         // 이 거리 밖이면 사격 안함
    public float fireCooldown = 1.25f;

    public Transform firePoint;                 // 총구/발사 위치(없으면 transform 사용)
    public GameObject projectilePrefab;         // 반드시 Rigidbody 포함
    public float projectileSpeed = 22f;         // 초기 속도 (m/s)
    public float projectileLifetime = 6f;       // 자동 파괴 시간(초)

    public bool faceToTargetWhenFire = true;    // 발사 순간 타겟을 바라보게 회전

    private float nextFireTime = 0f;            // 다음 공격까지의 쿨타임

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
        if (projectilePrefab == null) { Debug.LogWarning("[EnemyRangedAttack] projectilePrefab이 비어있습니다."); return; }

        // 3D 방향(대각 포함) 계산: 발사 지점 -> 타겟 지점
        Vector3 from = firePoint.position;
        Vector3 to = target.position;
        Vector3 dir = (to - from).normalized; // X/Y/Z 전체 사용

        if (faceToTargetWhenFire && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = look;
        }

        GameObject proj = Instantiate(projectilePrefab, from, Quaternion.LookRotation(dir, Vector3.up));

        // Rigidbody로 초기 속도 부여 (중력 사용여부는 프리팹의 Rigidbody 설정에 따름)
        Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {   rb.velocity = dir * projectileSpeed;    }      // 축 고정 없이 대각선 이동
            else
            {Debug.LogWarning("[EnemyRangedAttack] projectilePrefab에 Rigidbody가 없습니다. Rigidbody를 추가하세요.");}

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

