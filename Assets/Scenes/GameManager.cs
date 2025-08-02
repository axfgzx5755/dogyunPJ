using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int levelPoint = 1; // 시작 스테이지
    private int maxStage = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
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

    void LoadStageByPoint()
    {
        if (levelPoint <= maxStage)
        {
            string stageName = $"Stage{levelPoint}";
            Debug.Log($"스테이지 전환: {stageName}");
            SceneManager.LoadScene(stageName);
        }
    }
}