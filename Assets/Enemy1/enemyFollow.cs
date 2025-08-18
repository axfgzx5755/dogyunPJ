using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyFollow : MonoBehaviour
{
    public float speed = 3.5f;        // �̵� �ӵ�
    public float stopDistance = 1.0f; // ���ߴ� �Ÿ�
    public float turnSpeed = 10f;     // ȸ���� �ε巯��

    public string playerTag = "Player";
    public bool usePhysics = false;   // true�� Rigidbody�� �̵�, false�� Transform�� �̵�

    private Transform target;
    private Rigidbody rb;

    private void Awake()
    {
        if (usePhysics)
        {
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogWarning("[enemyFollow] usePhysics�� �������� Rigidbody�� �����ϴ�.");
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
        // Ÿ���� ������ٸ� ��Ž��
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null) 
                {   target = player.transform;  }
        }


        if (!usePhysics)
        { MoveTransform(Time.deltaTime); }          // Transform �̵� ���
    }

    private void FixedUpdate()
    {
        if (usePhysics)
        { MoveRigidbody(Time.fixedDeltaTime);   }   // Rigidbody �̵� ���
    }

    
    void MoveTransform(float dt)                    // Transform ��� �̵�
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

    
    void MoveRigidbody(float dt)                    // Rigidbody ��� �̵�
    {
        if (target == null || rb == null) return;

        Vector3 myPos = rb.position;
            Vector3 flatTargetPos = new Vector3(target.position.x, myPos.y, target.position.z);

        Vector3 dir = flatTargetPos - myPos;
            float dist = dir.magnitude;
        
        if (dist <= stopDistance) return;

        dir /= dist; // normalized

        RotateTowards(dir, dt);

        Vector3 step = dir * speed * dt;            // y�� ����(�߷�)�� ����ϵ��� ����
            Vector3 next = myPos + step;
                next.y = rb.position.y;

        rb.MovePosition(next);
    }

    
    void RotateTowards(Vector3 dir, float dt)       // ����: �ε巯�� ȸ��
    {
        if (dir.sqrMagnitude < 0.0001f) return;
            
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
    }
}
