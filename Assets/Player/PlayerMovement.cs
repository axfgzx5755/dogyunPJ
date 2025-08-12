using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Move/Jump")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundMask = ~0;   // Ground 레이어 지정 권장
    public float groundProbe = 0.15f;   // 바닥 체크 거리

    [Header("Animation")]
    public Animator animator;           // SpriteHolder(스프라이트가 있는 자식)의 Animator
    public SpriteRenderer sprite;       // 좌우반전(사이드뷰면 할당)
    public bool attackRandom = false;   // true=랜덤, false=1-2-1-2 순서
    int attackIndex = 0;                // 순차공격용

    Rigidbody rb;
    bool isGrounded = true;
    Vector3 inputDir;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // X/Z 회전 고정(넘어지는 것 방지)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (animator == null)
            Debug.LogWarning("Animator 미할당: SpriteHolder의 Animator를 할당하세요.");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        ReadInput();
        DriveAnimator();     // 애니메이션 파라미터 갱신
        HandleJump();
        HandleAttack();
        FlipSprite();        // 선택: 좌우 반전
    }

    void FixedUpdate()
    {
        MoveByPhysics();
        GroundCheck();
    }

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(h, 0f, v).normalized;
    }

    void MoveByPhysics()
    {
        if (inputDir.sqrMagnitude > 0f)
        {
            Vector3 target = rb.position + inputDir * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(target); // Translate 대신 물리 이동
        }
    }

    void DriveAnimator()
    {
        if (!animator) return;
        // 이동량을 0~1 범위로: 정지=0, 이동중≈1
        float speed = inputDir.sqrMagnitude; // 0 또는 1
        animator.SetFloat("Speed", speed);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Y 초기화
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;

            if (animator) animator.SetTrigger("jump");
        }
    }

    void HandleAttack()
{
    if (Input.GetMouseButtonDown(0))
    {
        if (!animator) return;

        // 랜덤으로 0 또는 1 선택
        int randIndex = Random.Range(1, 3);

        animator.SetInteger("PlayerAttackIndexAnimation", randIndex);
        animator.SetTrigger("attack");
    }
}

    void FlipSprite()
    {
        if (!sprite) return;            // 탑뷰면 생략 가능
        if (Mathf.Abs(inputDir.x) > 0.01f)
            sprite.flipX = (inputDir.x < 0f);
    }

    void GroundCheck()
    {
        // 콜라이더 하단에서 짧게 Raycast
        var col = GetComponent<Collider>();
        Vector3 origin = transform.position;
        float extY = 0.5f;
        if (col) { origin = col.bounds.center; extY = col.bounds.extents.y; }
        isGrounded = Physics.Raycast(origin, Vector3.down, extY + groundProbe, groundMask);
        // 디버그용: Debug.DrawLine(origin, origin + Vector3.down * (extY + groundProbe), Color.yellow);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 태그 방식도 병행 가능
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 스폰 포인트로 위치 리셋
        var spawn = GameObject.FindWithTag("PlayerSpawn");
        Vector3 pos = spawn ? spawn.transform.position : new Vector3(0, 1, 0);
        float up = 0.02f;
        var col = GetComponent<Collider>();
        if (col) up += col.bounds.extents.y;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(pos + Vector3.up * up, spawn ? spawn.transform.rotation : Quaternion.identity);
        Physics.SyncTransforms();
        rb.isKinematic = false;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
