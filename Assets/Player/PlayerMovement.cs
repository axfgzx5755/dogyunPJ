using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    private Rigidbody rb;
    private bool isGrounded = true;

    void Awake()
    {
        // 싱글톤 + DontDestroyOnLoad 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
            return;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogWarning("⚠️ Rigidbody가 Player에 없습니다.");

        // 현재 씬 로드 후 위치 초기화 처리
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAttack();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveDir = new Vector3(h, 0f, v).normalized;

        if (moveDir != Vector3.zero)
        {
            Debug.Log("📦 이동 중: " + moveDir);
        }

        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Debug.Log("🦘 점프!");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🗡️ 공격!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 씬 이동 시 위치 초기화 (옵션: 스폰 위치 등으로 수정 가능)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        transform.position = new Vector3(0, 1, 0); // 원하는 위치
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}