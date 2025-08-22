using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyFollow : MonoBehaviour
{
    public float speed = 3.5f;        // 이동 속도
    public float stopDistance = 1.0f; // 멈추는 거리
    public float turnSpeed = 10f;     // 회전의 부드러움

    public string playerTag = "Player";
    public bool usePhysics = false;   // true면 Rigidbody로 이동, false면 Transform로 이동

    private Transform target;
    private Rigidbody rb;

    private void Awake()
    {
        if (usePhysics)
        {
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogWarning("[enemyFollow] usePhysics가 켜졌지만 Rigidbody가 없습니다.");
            }
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {   target = player.transform;  }
        }
    }

    private void Update()
    {
        // 타겟이 사라졌다면 재탐색
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null) 
                {   target = player.transform;  }
        }


        if (!usePhysics)
        { MoveTransform(Time.deltaTime); }          // Transform 이동 모드
    }

    private void FixedUpdate()
    {
        if (usePhysics)
        { MoveRigidbody(Time.fixedDeltaTime);   }   // Rigidbody 이동 모드
    }

    
    void MoveTransform(float dt)                    // Transform 기반 이동
    {
        if (target == null) return;

        Vector3 myPos = transform.position;
            Vector3 flatTargetPos = new Vector3(target.position.x, myPos.y, target.position.z);

        Vector3 dir = flatTargetPos - myPos;
            float dist = dir.magnitude;
        
        if (dist <= stopDistance) return;

        dir /= dist; // normalized

        RotateTowards(dir, dt);
            transform.position = Vector3.MoveTowards(myPos, myPos + dir, speed * dt);
    }

    
    void MoveRigidbody(float dt)                    // Rigidbody 기반 이동
    {
        if (target == null || rb == null) return;

        Vector3 myPos = rb.position;
            Vector3 flatTargetPos = new Vector3(target.position.x, myPos.y, target.position.z);

        Vector3 dir = flatTargetPos - myPos;
            float dist = dir.magnitude;
        
        if (dist <= stopDistance) return;

        dir /= dist; // normalized

        RotateTowards(dir, dt);

        Vector3 step = dir * speed * dt;            // y는 물리(중력)가 담당하도록 유지
            Vector3 next = myPos + step;
                next.y = rb.position.y;

        rb.MovePosition(next);
    }

    
    void RotateTowards(Vector3 dir, float dt)       // 공통: 부드러운 회전
    {
        if (dir.sqrMagnitude < 0.0001f) return;
            
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
    }
}
