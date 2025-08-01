using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed;
    public float groundDist;        // ���� ���̾�� �÷��̾ �󸶳� ����� ����
    public float jumpForce = 5f;    // �����Ҷ� ��ũ��
    
    public LayerMask testMap;       // ���� ���̾� ����
    public Rigidbody rb;            // Rigidbody ������Ʈ      | ������� �̵�ó��
    public SpriteRenderer sr;       // SpriteRenderer ������Ʈ | ��������Ʈ ������ȯ

    private bool isGrounded = false; // ���鿡 �ִ��� ����Ȯ�� | ���� ���� ������

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        RaycastHit hit;
        Vector3 castPos = transform.position;
            castPos.y += 1;
        isGrounded = false;
        // �÷��̾ ���� ���� ���ִ� ��ǥ ������
        if (Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, testMap))
        {
            if (hit.collider != null)
            {
                Vector3 movePos = transform.position;
                    movePos.y = hit.point.y + groundDist;
                transform.position = movePos;
                
                isGrounded = true;
            }
        }
        
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
            Vector3 moveDir = new Vector3(x, 0, y);     // ������� ����
        // y�� �ӵ��� �����ϸ鼭 ���� �̵�
        rb.velocity = new Vector3(moveDir.x * speed, rb.velocity.y, moveDir.z * speed);

        // ���� ó��
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {   rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);   }

        // ��������Ʈ ���� ��ȯ��
        if (x != 0 && x < 0)
        {
            sr.flipX = true;                            // flipX -> ����sr ����
        }
        else if (x != 0 && x > 0)
        {
            sr.flipX = false;
        }
    }
}
