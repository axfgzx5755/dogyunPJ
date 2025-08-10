using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryButton : MonoBehaviour
{
    // 버튼에서 호출할 메서드
    public void Retry()
    {
        // "StartMap" 씬으로 이동 (대소문자 정확히 일치!)
        SceneManager.LoadScene("StartMap");
    }
}