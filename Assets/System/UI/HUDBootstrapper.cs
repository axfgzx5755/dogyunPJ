using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // ← 이거 추가!
using UnityEngine.SceneManagement;

public class HUDBootstrapper : MonoBehaviour
{
    public static HUDBootstrapper Instance;

    public Canvas hudCanvasPrefab;         // 선택
    public GameObject characterHUDPrefab;  // 필수(2단계에서 만든 프리팹)

    Canvas canvas;
    GameObject hud;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!canvas)
        {
            if (hudCanvasPrefab) canvas = Instantiate(hudCanvasPrefab);
            else
            {
                var go = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            DontDestroyOnLoad(canvas.gameObject);
        }

        if (!hud && characterHUDPrefab)
        {
            hud = Instantiate(characterHUDPrefab, canvas.transform);
            DontDestroyOnLoad(hud);
        }
    }
}