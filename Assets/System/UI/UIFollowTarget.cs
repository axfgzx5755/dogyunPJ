using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIFollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    public bool clampToScreen = true;
    public float edgePadding = 8f;
    public bool hideWhenBehind = true;

    RectTransform rt;
    Camera cam;
    Canvas canvas;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        AcquireCamera();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        if (!target && PlayerMovement.Instance)
            target = PlayerMovement.Instance.transform;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        AcquireCamera();
        if (PlayerMovement.Instance && (!target || !target.gameObject.scene.IsValid()))
            target = PlayerMovement.Instance.transform;
    }

    void AcquireCamera() => cam = Camera.main;

    void LateUpdate()
    {
        if (!target || !rt) return;
        if (!cam) AcquireCamera();

        Vector3 sp = cam ? cam.WorldToScreenPoint(target.position + worldOffset)
                         : new Vector3(Screen.width*0.5f, Screen.height*0.5f, 1f);

        if (hideWhenBehind && sp.z < 0f)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        else if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (clampToScreen)
        {
            sp.x = Mathf.Clamp(sp.x, edgePadding, Screen.width  - edgePadding);
            sp.y = Mathf.Clamp(sp.y, edgePadding, Screen.height - edgePadding);
        }

        rt.position = sp;
    }
}