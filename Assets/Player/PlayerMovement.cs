using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Move/Jump")]
    [SerializeField] float moveThreshold = 5f;      // 이동 속도(유닛/초)
    [SerializeField] float jumpSpeed = 7f;          // 점프 상승 속도(ForceMode.VelocityChange)
    [SerializeField] LayerMask groundMask = ~0;     // '땅'으로 인식할 레이어
    [SerializeField] float spawnGraceTime = 0.15f;  // 씬 로드 직후 판정 보류(물리 안정화)
    float spawnedAt;
    bool justSpawned;

    [Header("Jump Assist")]
    [SerializeField] float coyoteTime = 0.12f;      // 땅에서 잠깐 떨어져도 점프 허용
    [SerializeField] float jumpBufferTime = 0.12f;  // 키를 살짝 일찍 눌러도 점프 허용
    float lastGroundedTime = -999f;                 // 최근 접지 시간
    float lastJumpPressedTime = -999f;              // 최근 점프키 입력 시간

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
    static readonly int HashR     = Animator.StringToHash("R");

    // Components / state
    Rigidbody rb;
    Collider col;
    bool isGrounded = true;      // GroundCheck로 매 프레임 갱신
    Vector3 inputDir;            // 입력 방향(정규화)

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

        // ⬇ 점프 입력은 "기록"만 + 그 순간 진단 로그 출력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;

            // 진단: 발 아래에 실제로 뭐가 있는지(모든 레이어) 한 번 찍어줌
            if (col)
            {
                var b = col.bounds;
                Vector3 feet = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);
                if (Physics.Raycast(feet + Vector3.up * 0.01f, Vector3.down, out RaycastHit rHit, 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    int layer = rHit.collider.gameObject.layer;
                    bool inMask = (groundMask.value & (1 << layer)) != 0;
                    Debug.Log($"[Jump KeyDown] grounded={isGrounded}, canCoyote={(Time.time - lastGroundedTime)<=coyoteTime} " +
                              $"sinceGrounded={(Time.time - lastGroundedTime):F3}s, buffer={jumpBufferTime}s, " +
                              $"kin={rb.isKinematic}, grav={rb.useGravity}, velY={rb.velocity.y:F4} || " +
                              $"UNDER='{rHit.collider.name}' layer='{LayerMask.LayerToName(layer)}' inMask={inMask} dist={rHit.distance:F3}");
                }
                else
                {
                    Debug.Log($"[Jump KeyDown] grounded={isGrounded}, canCoyote={(Time.time - lastGroundedTime)<=coyoteTime} " +
                              $"sinceGrounded={(Time.time - lastGroundedTime):F3}s, buffer={jumpBufferTime}s, " +
                              $"kin={rb.isKinematic}, grav={rb.useGravity}, velY={rb.velocity.y:F4} || UNDER=None (no collider within 2.0)");
                }
            }
        }

        HandleAttack();  // 좌클릭 → 공격(인덱스 + 트리거)
        HandleR();       // R → 기타 트리거
        UpdateFacing();  // 좌/우 바라보기 갱신
    }

    void FixedUpdate()
    {
        GroundCheck();   // 접지 판정
        MoveByPhysics(); // 물리 이동
        TryJump();       // 버퍼/코요테 반영한 점프 실행
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

    /// <summary>버퍼/코요테 기반 실제 점프 실행(속도 방식)</summary>
    void TryJump()
    {
        // 씬 로드 직후 그레이스 타임 동안 점프 실행 보류
        if (justSpawned && (Time.time - spawnedAt) < spawnGraceTime)
            return;
        justSpawned = false;

        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBuffered = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (hasBuffered && canCoyote)
        {
            // 수직 속도 0으로 초기화 후 질량 무시 속도 부여 → 확정 점프
            var v = rb.velocity; v.y = 0f; rb.velocity = v;
            rb.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);
            isGrounded = false;

            if (animator) animator.SetTrigger(HashJump);

            // 버퍼 소모
            lastJumpPressedTime = -999f;

            Debug.Log($"[Jump Execute] jumpSpeed={jumpSpeed}, velY={rb.velocity.y:F4}");
        }
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
            Debug.Log($"[Attack] idx={idx}");
        }
    }

    /// <summary>R키 트리거</summary>
    void HandleR()
    {
        if (Input.GetKeyDown(KeyCode.R) && animator)
            animator.SetTrigger(HashR);
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

    #region Grounding
    /// <summary>
    /// ✅ 경량·안정 접지 판정: CheckSphere(발 주변) → 실패 시 Raycast(발 아래)
    ///  - 마스크 미스/미세 간극/경계 프레임에 강함
    /// </summary>
    void GroundCheck()
    {
        if (!col) { isGrounded = false; return; }

        var b = col.bounds;
        Vector3 feet = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);

        // 1) 발 주변 겹침으로 빠르게 확인 (작은 반경)
        float r = Mathf.Max(0.08f, Mathf.Min(b.extents.x, b.extents.z) * 0.48f);
        bool grounded = Physics.CheckSphere(feet, r, groundMask, QueryTriggerInteraction.Ignore);

        // 2) 여전히 아니면, 바로 아래로 레이 쏴서 짧은 거리 내 지면 감지
        if (!grounded)
        {
            const float maxDown = 0.3f;   // 발아래 허용 간격
            if (Physics.Raycast(feet + Vector3.up * 0.01f, Vector3.down, out RaycastHit hit, maxDown, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 위쪽 법선(평면/완만한 경사)만 접지로 인정
                if (hit.normal.y > 0.25f) grounded = true;
            }
        }

        isGrounded = grounded;
        if (grounded) lastGroundedTime = Time.time;
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

        // 4) 바로 접지 판정 1회(초기 상태 확정)
        GroundCheck();

        // 5) 물리 재개(원래 상태로 복원)
        rb.isKinematic = wasKinematic;

        // 6) 스폰 그레이스 타임 시작
        spawnedAt = Time.time;
        justSpawned = true;

        Debug.Log($"[Spawn] grounded={isGrounded} at {transform.position}");
    }
    #endregion
}
