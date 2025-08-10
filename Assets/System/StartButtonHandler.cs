using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartButtonHandler : MonoBehaviour
{
    public Button StartButton;

    void Start()
    {
        StartButton.onClick.AddListener(OnStartButtonClicked);
        
    }

    public void OnStartButtonClicked()
    {
        Debug.Log("Start 버튼이 눌렸습니다 (스크립트 연결)");
        GameManager.Instance.StartGame();
    }
}