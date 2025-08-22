using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;   // 최대 체력
    private int currentHealth;    // 현재 체력

    void Start()
    {
        currentHealth = maxHealth; // 시작 시 체력 풀로 채움
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            currentHealth = 0;
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    // 다른 스크립트에서 데미지를 줄 때 호출
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Player HP: " + currentHealth);
    }

    void Die()
    {
        // 플레이어 오브젝트 삭제
        Destroy(gameObject);

        // FailScene 로드
        SceneManager.LoadScene("Failscenes"); 
        // "FailScene"은 빌드 세팅에 추가되어 있어야 함
    }
}