using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


public class CharacterUIBinder : MonoBehaviour
{
    [Header("UI")]
    public Slider healthBar;
    public TextMeshProUGUI ammoText;
    public RectTransform extraSlot;

    [Header("Targets (auto if empty)")]
    public PlayerHealth healthTarget;
    public PlayerAmmo ammoTarget;

    void Awake()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void Start()  { TryAutoBind(); InitHealthBar(); RefreshAll(); }
    void OnSceneLoaded(Scene s, LoadSceneMode m) { TryAutoBind(); InitHealthBar(); }

    void Update() { RefreshAll(); } // 간단 폴링

    void TryAutoBind()
    {
        if (!PlayerMovement.Instance) return;
        if (!healthTarget) healthTarget = PlayerMovement.Instance.GetComponent<PlayerHealth>();
        if (!ammoTarget)   ammoTarget   = PlayerMovement.Instance.GetComponent<PlayerAmmo>();
    }

    void InitHealthBar()
    {
        if (healthBar && healthTarget)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = Mathf.Max(1, healthTarget.maxHealth);
        }
    }

    void RefreshAll()
    {
        if (healthBar && healthTarget)
        {
            // PlayerHealth에 CurrentHP 게터 추가 필요(아래 4단계 참고)
            healthBar.value = Mathf.Clamp(healthTarget.currentHealth, 0, healthTarget.maxHealth);
        }
        if (ammoText && ammoTarget)
        {
            ammoText.text = $"{ammoTarget.currentAmmo} / {ammoTarget.reserveAmmo}";
        }
    }

    public void AttachToExtra(GameObject widget)
    {
        if (!extraSlot || !widget) return;
        var rt = widget.transform as RectTransform;
        rt.SetParent(extraSlot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }
}