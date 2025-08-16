using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableBoxSpawner : MonoBehaviour
{
    [Header("스폰 위치/크기")]
    public bool autoSpawnOnStart = false;                 // 씬 시작 시 자동 스폰 여부
    public Vector3 spawnPosition = new Vector3(2, 4f, 2); // 높이 2니까 바닥(y=0)에 올리려면 중심 y=1
    public Vector3 boxSize = new Vector3(1f, 2f, 1f);     // Y=2

    [Header("물리 세팅")]
    public float mass = 5f;
    public float drag = 1f;         // 관성 줄여서 멈추기 좋게
    public float angularDrag = 0.5f;
    public bool preventTipping = true; // 앞으로/옆으로 넘어지지 않게

    void Start()
    {
        if (autoSpawnOnStart)
            SpawnPushableBox();
    }

    // GameManager에서 호출할 공개 메서드
    public GameObject SpawnPushableBox()
    {
        // 기본 큐브 생성
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "PushableBox";
        cube.transform.position = spawnPosition;
        cube.transform.localScale = boxSize;

        // 물리 컴포넌트
        var rb = cube.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        if (preventTipping)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // 마찰 조정 (선택)
        var col = cube.GetComponent<BoxCollider>();
        var mat = new PhysicMaterial("BoxFriction");
        mat.dynamicFriction = 0.6f;  // 미는 느낌 유지하면서 미끄럼 과하지 않게
        mat.staticFriction  = 0.8f;
        mat.bounciness      = 0f;
        mat.frictionCombine = PhysicMaterialCombine.Average;
        col.material = mat;

        return cube;
    }
}