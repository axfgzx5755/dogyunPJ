using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    // ★ 탄약 컴포넌트 참조
    private PlayerAmmo ammo;

    [Header("Move/Jump")]
    [SerializeField] float moveThreshold = 5f;      // 이동 속도(유닛/초)
    [SerializeField] float jumpSpeed = 7f;          // 점프 힘(Impulse에 사용)
    [SerializeField] LayerMask groundMask = ~0;     // '땅'으로 인식할 레이어
    [SerializeField] float groundCheckDist = 0.25f; // 발 아래 체크 거리
    [SerializeField] float spawnGraceTime = 0.15f;  // 로드 직후 입력/점프 보류
    float spawnedAt;
    bool justSpawned;

    [Header("Animation")]
    [SerializeField] Animator animator;                           // 자동 탐색됨
    [SerializeField] RuntimeAnimatorController player1Animation;  // 기본 컨트롤러
    [SerializeField] SpriteRenderer sprite;                       // 2D 사이드뷰면 할당

    // Animator parameter keys / hashes
    const string ParamIsMove      = "isMove";            // Bool
    static readonly int HashJump  = Animator.StringToHash("jump");
    const string ParamAttack      = "attack";            // Trigger
    const string ParamAttackIndex = "AttackIndex";       // Int
    const string ParamReload      = "R";                 // 재장전 트리거

    // Components / state
    Rigidbody rb;
    Collider col;
    bool isGrounded = true;
    Vector3 inputDir;
    bool jumpPressed;

    // 바라보기(좌/우)
    float faceDir = 1f;          // +1: 오른쪽, -1: 왼쪽
    Vector3 baseScale;
    [SerializeField] Transform visualRoot;

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        ammo = GetComponent<PlayerAmmo>(); // ★ 추가

        // 물리 세팅
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 애니메이터 자동 할당/강제 설정
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator && player1Animation && animator.runtimeAnimatorController != player1Animation)
            animator.runtimeAnimatorController = player1Animation;

        if (!visualRoot) visualRoot = sprite ? sprite.transform : transform;
        baseScale = visualRoot.localScale;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        ReadInput();
        MoveBool();

        // 점프 입력 기록(실행은 FixedUpdate)
        if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;

        // ★ 입력 위임: 공격/재장전은 탄약 시스템으로
        if (Input.GetMouseButtonDown(0)) ammo?.Shoot();
        if (Input.GetKeyDown(KeyCode.R))   ammo?.Reload();

        UpdateFacing();
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        MoveByPhysics();
        TryJumpSimple();
    }
    #endregion

    #region Animation API (외부 호출용)
    /// <summary>공격 애니메이션 실행 (1=빈 총, 2=실사격)</summary>
    public void PlayAttack(int idx)
    {
        if (!animator) return;
        animator.SetInteger(ParamAttackIndex, idx);
        animator.SetTrigger(ParamAttack);
    }

    /// <summary>재장전 애니메이션 트리거</summary>
    public void PlayReload()
    {
        if (!animator) return;
        animator.SetTrigger(ParamReload);
    }
    #endregion

    #region Input / Move / Jump
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
            Vector3 target = rb.position + inputDir * moveThreshold * Time.fixedDeltaTime;
            rb.MovePosition(target);
        }
    }

    void MoveBool()
    {
        if (!animator) return;
        animator.SetBool(ParamIsMove, inputDir.sqrMagnitude > 0f);
    }

    void TryJumpSimple()
    {
        if (justSpawned && (Time.time - spawnedAt) < spawnGraceTime)
        {
            jumpPressed = false;
            return;
        }
        justSpawned = false;

        if (jumpPressed && isGrounded)
        {
            // 수직 속도 정리 후 Impulse 점프
            Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
            rb.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
            isGrounded = false;

            if (animator) animator.SetTrigger(HashJump);
        }

        // 입력 소모
        jumpPressed = false;
    }
    #endregion

    #region Facing
    void UpdateFacing()
    {
        if (Mathf.Abs(inputDir.x) > 0.01f)
            faceDir = Mathf.Sign(inputDir.x);

        if (sprite) // 2D 스프라이트
        {
            sprite.flipX = (faceDir < 0f);
        }
        else        // 3D/메쉬 → 회전으로 방향 전환
        {
            Vector3 e = transform.localEulerAngles;
            e.y = (faceDir < 0f) ? 180f : 0f;
            transform.localEulerAngles = e;
        }
    }
    #endregion

    #region Ground
    void UpdateGrounded()
    {
        if (!col) { isGrounded = false; return; }

        var b = col.bounds;
        Vector3 feet = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);

        if (Physics.Raycast(feet + Vector3.up * 0.01f, Vector3.down,
                            out RaycastHit hit, groundCheckDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    #endregion

    #region Scene Spawn
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = GameObject.FindWithTag("Respawn");
        Vector3 pos = spawn ? spawn.transform.position : new Vector3(0, 2, 0);
        Quaternion rot = spawn ? spawn.transform.rotation : Quaternion.identity;

        bool wasKinematic = rb.isKinematic;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;

        float up = (col ? col.bounds.extents.y : 0.5f) + 0.10f;
        transform.SetPositionAndRotation(pos + Vector3.up * up, rot);
        Physics.SyncTransforms();

        UpdateGrounded();

        rb.isKinematic = wasKinematic;

        spawnedAt = Time.time;
        justSpawned = true;
    }
    #endregion
}
