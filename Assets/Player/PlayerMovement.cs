using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Move/Jump")]
    [SerializeField] float moveThreshold = 5f;      // 이동 속도(유닛/초)
    [SerializeField] float jumpSpeed = 7f;          // 점프 힘(Impulse에 사용)
    [SerializeField] LayerMask groundMask = ~0;     // '땅'으로 인식할 레이어(반드시 Ground 전용)
    [SerializeField] float groundCheckDist = 0.25f; // 발 아래 짧은 거리만 체크
    [SerializeField] float spawnGraceTime = 0.15f;  // 씬 로드 직후 입력/점프 보류(물리 안정화)
    float spawnedAt;
    bool justSpawned;

    [Header("Animation")]
    [SerializeField] Animator animator;                           // 자동 탐색됨
    [SerializeField] RuntimeAnimatorController player1Animation;  // 기본 컨트롤러
    [SerializeField] SpriteRenderer sprite;                       // 2D 사이드뷰면 할당
    [SerializeField] bool attackRandom = true;                    // 공격 인덱스 랜덤 여부

    // Animator parameter keys / hashes
    const string ParamIsMove      = "isMove";      // Bool
    static readonly int HashJump  = Animator.StringToHash("jump");
    const string ParamAttack      = "attack";      // Trigger
    const string ParamAttackIndex = "AttackIndex"; // Int
    static readonly int HashR     = Animator.StringToHash("R");   // (선택) 사용 중이면 유지
    const string ParamReload      = "R";      // 재장전 트리거

    // Components / state
    Rigidbody rb;
    Collider col;
    bool isGrounded = true;      // 매 FixedUpdate에서 Raycast로 갱신
    Vector3 inputDir;            // 입력 방향(정규화)
    bool jumpPressed;            // Update에서 입력만 기록

    // 바라보기(좌/우)
    float faceDir = 1f;          // +1: 오른쪽, -1: 왼쪽
    Vector3 baseScale;
    [SerializeField] Transform visualRoot; // 스프라이트/메쉬 최상위(없으면 자동)

    #region Unity Lifecycle
    void Awake()
    {
        // 싱글턴(씬 이동 시 유지)
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // 물리 안정화
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 애니메이터 자동 할당/강제 설정
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator && player1Animation && animator.runtimeAnimatorController != player1Animation)
            animator.runtimeAnimatorController = player1Animation;

        // 시각 루트/스케일 초기화
        if (!visualRoot) visualRoot = sprite ? sprite.transform : transform;
        baseScale = visualRoot.localScale;

        // 씬 로드 시 스폰 처리
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        ReadInput();     // 입력 읽기
        MoveBool();      // 이동 여부 → 애니 파라미터

        // 점프 입력은 기록만(실제 실행은 FixedUpdate에서)
        if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;

        HandleAttack();  // 좌클릭 → 공격(인덱스 + 트리거)
        HandleR();       // R → 재장전(playerReloadAnimation)
        UpdateFacing();  // 좌/우 바라보기 갱신
    }

    void FixedUpdate()
    {
        UpdateGrounded();  // 간단 접지 판정(Raycast 1회)
        MoveByPhysics();   // 물리 이동(원래 방식 유지)
        TryJumpSimple();   // 예시 방식(Impulse)으로 점프 실행
    }
    #endregion

    #region Input / Move / Jump
    /// <summary>수평/수직 입력을 정규화해 기록</summary>
    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(h, 0f, v).normalized;
    }

    /// <summary>Rigidbody.MovePosition으로 부드러운 물리 이동</summary>
    void MoveByPhysics()
    {
        if (inputDir.sqrMagnitude > 0f)
        {
            Vector3 target = rb.position + inputDir * moveThreshold * Time.fixedDeltaTime;
            rb.MovePosition(target);
        }
    }

    /// <summary>애니메이터에 이동 여부를 전달</summary>
    void MoveBool()
    {
        if (!animator) return;
        animator.SetBool(ParamIsMove, inputDir.sqrMagnitude > 0f);
    }

    /// <summary>간단 점프: 접지 + 입력 시 Impulse로 상승</summary>
    void TryJumpSimple()
    {
        // 씬 로드 직후 그레이스 타임 동안 점프 금지(필요시 제거 가능)
        if (justSpawned && (Time.time - spawnedAt) < spawnGraceTime)
        {
            jumpPressed = false;
            return;
        }
        justSpawned = false;

        if (jumpPressed && isGrounded)
        {
            // 수직 속도 0으로 정리 후 Impulse로 점프
            Vector3 v = rb.velocity;
            v.y = 0f; 
            rb.velocity = v;

            rb.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
            isGrounded = false;

            if (animator) animator.SetTrigger(HashJump);
        }

        // 입력 소모
        jumpPressed = false;
    }
    #endregion

    #region Combat / Triggers
    /// <summary>좌클릭: 공격 인덱스(1/2) 세팅 후 공격 트리거</summary>
    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && animator)
        {
            int idx = attackRandom ? Random.Range(1, 3) : 1; // 1 또는 2
            animator.SetInteger(ParamAttackIndex, idx);
            animator.SetTrigger(ParamAttack);
        }
    }

    /// <summary>R키 → 재장전 모션 실행</summary>
    void HandleR()
    {
        if (Input.GetKeyDown(KeyCode.R) && animator)
        {
            animator.SetTrigger(ParamReload);
        }
    }
    #endregion

    #region Facing
    /// <summary>좌/우 입력에 따라 마지막 방향을 유지하며 플립</summary>
    void UpdateFacing()
    {
        // 입력이 있을 때만 방향 갱신
        if (Mathf.Abs(inputDir.x) > 0.01f)
            faceDir = Mathf.Sign(inputDir.x); // -1 또는 +1

        if (sprite) // 2D 스프라이트 → flipX
        {
            sprite.flipX = (faceDir < 0f);
        }
        else        // 3D/메쉬 → 음수 스케일 금지! Y=0/180 회전으로 방향 전환
        {
            Vector3 e = transform.localEulerAngles;
            e.y = (faceDir < 0f) ? 180f : 0f;
            transform.localEulerAngles = e;
        }
    }
    #endregion

    #region Ground (간단 판정만 사용)
    /// <summary>
    /// 예시 스타일: 발 아래로 짧은 Raycast 한 번으로만 접지 판정.
    /// - 무한 거리 금지(Mathf.Infinity X). 너무 길면 공중에서도 맞음.
    /// </summary>
    void UpdateGrounded()
    {
        if (!col) { isGrounded = false; return; }

        var b = col.bounds;
        Vector3 feet = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);

        if (Physics.Raycast(feet + Vector3.up * 0.01f, Vector3.down,
                            out RaycastHit hit, groundCheckDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            // (선택) 너무 가파른 경사는 제외하고 싶으면 사용
            // if (hit.normal.y <= 0.2f) { isGrounded = false; return; }

            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    #endregion

    #region Scene Spawn
    /// <summary>씬 로드시 Respawn 태그 위치로 스폰(키네마틱/속도 순서 주의) + 그레이스 타임</summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = GameObject.FindWithTag("Respawn");
        Vector3 pos = spawn ? spawn.transform.position : new Vector3(0, 2, 0);
        Quaternion rot = spawn ? spawn.transform.rotation : Quaternion.identity;

        // 0) 기존 키네마틱 상태 백업
        bool wasKinematic = rb.isKinematic;

        // 1) 속도 리셋(키네마틱 켜기 전에!)
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2) 순간 워프 위해 잠시 kinematic ON
        rb.isKinematic = true;

        // 3) 대략적 위치로 이동(콜라이더 절반 높이 + 여유)
        float up = (col ? col.bounds.extents.y : 0.5f) + 0.10f;
        transform.SetPositionAndRotation(pos + Vector3.up * up, rot);
        Physics.SyncTransforms();

        // 4) 초기 접지 추정
        UpdateGrounded();

        // 5) 물리 재개(원래 상태로 복원)
        rb.isKinematic = wasKinematic;

        // 6) 스폰 그레이스 타임 시작
        spawnedAt = Time.time;
        justSpawned = true;
    }
    #endregion
}
