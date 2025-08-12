using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Move/Jump")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundMask = ~0;
    public float groundProbe = 0.15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;                             // 자동 탐색됨
    [SerializeField] private RuntimeAnimatorController player1Animation;    // 기본 컨트롤러
    public SpriteRenderer sprite;                                           // 사이드뷰면 할당
    public bool attackRandom = true;

    // Animator parameter hashes
    static readonly int HashSpeed       = Animator.StringToHash("Speed");
    static readonly int HashJump        = Animator.StringToHash("jump");
    static readonly int HashAttack      = Animator.StringToHash("attack");
    static readonly int HashAttackIndex = Animator.StringToHash("attackIndex");
    static readonly int HashR           = Animator.StringToHash("R");

    Rigidbody rb;
    Collider col;
    bool isGrounded = true;
    Vector3 inputDir;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator && player1Animation && animator.runtimeAnimatorController != player1Animation)
            animator.runtimeAnimatorController = player1Animation;

        SceneManager.sceneLoaded += OnSceneLoaded;
        if (!animator) Debug.LogWarning("[PlayerMovement] Animator가 없습니다.");
        if (!player1Animation) Debug.LogWarning("[PlayerMovement] player1Animation 컨트롤러를 할당하세요.");
    }

    void Update()
    {
        ReadInput();
        DriveAnimator();     // Speed 갱신
        HandleJump();        // Space → jump
        HandleAttack();      // LMB → attack + attackIndex(1/2)
        HandleR();           // R → R
        FlipSprite();        // 선택
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
            rb.MovePosition(target);
        }
    }

    // Speed 파라미터 0~1로 반영
    void DriveAnimator()
    {
        if (!animator) return;
        animator.SetFloat(HashSpeed, inputDir.magnitude);
    }

    // Space → jump
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            if (animator) animator.SetTrigger(HashJump);
        }
    }

    // 좌클릭 → attack + attackIndex(1/2)
    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && animator)
        {
            int idx = attackRandom ? Random.Range(1, 3) : 1; // 1 또는 2
            animator.SetInteger(HashAttackIndex, idx);
            animator.SetTrigger(HashAttack);
        }
    }

    // R → R 트리거
    void HandleR()
    {
        if (Input.GetKeyDown(KeyCode.R) && animator)
            animator.SetTrigger(HashR);
    }

    void FlipSprite()
    {
        if (!sprite) return;
        if (Mathf.Abs(inputDir.x) > 0.01f)
            sprite.flipX = (inputDir.x < 0f);
    }

    void GroundCheck()
    {
        Vector3 origin = col ? col.bounds.center : transform.position;
        float extY = col ? col.bounds.extents.y : 0.5f;
        isGrounded = Physics.Raycast(origin, Vector3.down, extY + groundProbe, groundMask);
        // Debug.DrawLine(origin, origin + Vector3.down * (extY + groundProbe), Color.yellow);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = GameObject.FindWithTag("PlayerSpawn");
        Vector3 pos = spawn ? spawn.transform.position : new Vector3(0, 1, 0);
        float up = 0.02f + (col ? col.bounds.extents.y : 0.5f);

        rb.isKinematic = true;
        rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(pos + Vector3.up * up, spawn ? spawn.transform.rotation : Quaternion.identity);
        Physics.SyncTransforms();
        rb.isKinematic = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
