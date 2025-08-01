using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed;
    public float groundDist;        // 지형 레이어에서 플레이어를 얼마나 띄울지 설정
    public float jumpForce = 5f;    // 점프할때 힘크기
    
    public LayerMask testMap;       // 지형 레이어 설정
    public Rigidbody rb;            // Rigidbody 컴포넌트      | 물리기반 이동처리
    public SpriteRenderer sr;       // SpriteRenderer 컴포넌트 | 스프라이트 방향전환

    private bool isGrounded = false; // 지면에 있는지 여부확인 | 공중 점프 방지용

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
        // 플레이어가 지면 위에 떠있는 좌표 방지용
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
            Vector3 moveDir = new Vector3(x, 0, y);     // 수평방향 벡터
        // y축 속도를 유지하면서 수평 이동
        rb.velocity = new Vector3(moveDir.x * speed, rb.velocity.y, moveDir.z * speed);

        // 점프 처리
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {   rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);   }

        // 스프라이트 방향 전환용
        if (x != 0 && x < 0)
        {
            sr.flipX = true;                            // flipX -> 변수sr 참조
        }
        else if (x != 0 && x > 0)
        {
            sr.flipX = false;
        }
    }
}
