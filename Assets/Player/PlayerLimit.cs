using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLimit : MonoBehaviour
{
    public float minX = -20f;
    public float maxX = 20f;
    public float minZ = -10f;
    public float maxZ = 10f;
    public float minY = 0f;  // 바닥 아래로 안 빠지게

    void Update()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Max(pos.y, minY);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }
}
