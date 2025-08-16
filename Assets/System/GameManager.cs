using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int levelPoint = 0; // 시작 스테이지
    private int maxStage = 3;

    public GameObject player;
    public GameObject cameraObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 플레이어와 카메라 유지
            if (player == null)
                player = GameObject.FindWithTag("Player");

            if (player != null)
                DontDestroyOnLoad(player);

            if (cameraObject == null)
                cameraObject = GameObject.FindWithTag("MainCamera");

            if (cameraObject != null)
                DontDestroyOnLoad(cameraObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // 몬스터 처치 테스트용: P키로 포인트 증가
        if (Input.GetKeyDown(KeyCode.P))
        {
            levelPoint++;
            LoadStageByPoint();
        }
    }

    public void StartGame()
    {
        levelPoint++;
        LoadStageByPoint();
    }

    void LoadStageByPoint()
    {
        if (levelPoint <= maxStage)
        {
            string stageName = $"Stage{levelPoint}";
            SceneManager.LoadScene(stageName);
        }
        else
        {
            Destroy(player);
            SceneManager.LoadScene("EndScene");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로딩 후 다시 플레이어와 카메라 찾기
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (cameraObject == null)
            cameraObject = GameObject.FindWithTag("MainCamera");

        if (cameraObject != null && player != null)
        {
            FollowCamera follow = cameraObject.GetComponent<FollowCamera>();
            if (follow != null)
            {
                follow.target = player.transform;
            }
        }
            var spawner = FindObjectOfType<PushableBoxSpawner>();
            if (spawner != null)
            {
            Debug.Log("박스스폰");
                spawner.SpawnPushableBox();
            }
    }
}