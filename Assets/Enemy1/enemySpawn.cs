using UnityEngine;
using System.Collections;

[AddComponentMenu("KDGame/Spawner/enemySpawn")]
public class enemySpawn : MonoBehaviour
{
    [System.Serializable]
    public struct AxisRange
    {
        public float min;
        public float max;

        public float Sample() => Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
    }

    [System.Serializable]
    public struct SpawnRange
    {
        [Header("X / Y / Z 최소·최대")]
        public AxisRange x;
        public AxisRange y;
        public AxisRange z;

        public Vector3 SamplePosition()
        {
            return new Vector3(x.Sample(), y.Sample(), z.Sample());
        }

        public Vector3 Center =>
            new Vector3((x.min + x.max) * 0.5f, (y.min + y.max) * 0.5f, (z.min + z.max) * 0.5f);

        public Vector3 Size =>
            new Vector3(Mathf.Abs(x.max - x.min), Mathf.Abs(y.max - y.min), Mathf.Abs(z.max - z.min));
    }

    [Header("Spawn Ranges (둘 중 하나를 매번 랜덤 선택)")]
    public SpawnRange spawnRange1;
    public SpawnRange spawnRange2;

    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Timing")]
    [Min(0)] public float initialDelay = 0f;
    [Min(0.01f)] public float spawnInterval = 3f;

    [Header("Count")]
    [Min(0)] public int totalToSpawn = 10;

    [Header("Options")]
    public bool autoStart = true;
    public bool useSpawnerRotation = true; // 이 컴포넌트의 회전 사용
    public bool randomYaw = false;         // Y축 랜덤 회전

    [Header("Debug (읽기 전용)")]
    [SerializeField] private int spawnedCount = 0;

    private Coroutine routine;

    private void OnEnable()
    {
        if (autoStart && routine == null)
            routine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    public void StartSpawning()
    {
        if (routine == null) routine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[enemySpawn] enemyPrefab이 비어 있습니다.", this);
            yield break;
        }

        spawnedCount = 0;

        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (spawnedCount < totalToSpawn)
        {
            SpawnOne();
            spawnedCount++;

            if (spawnedCount >= totalToSpawn) break;
            yield return new WaitForSeconds(spawnInterval);
        }

        routine = null;
    }

    private void SpawnOne()
    {
        // 1, 2 중 하나의 범위를 랜덤 선택
        bool useFirst = Random.value < 0.5f;
        Vector3 pos = (useFirst ? spawnRange1 : spawnRange2).SamplePosition();

        Quaternion rot = useSpawnerRotation ? transform.rotation : Quaternion.identity;
        if (randomYaw)
        {
            rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * rot;
        }

        Instantiate(enemyPrefab, pos, rot);
    }

    private void OnValidate()
    {
        if (spawnInterval < 0.01f) spawnInterval = 0.01f;
        if (initialDelay < 0f) initialDelay = 0f;
        if (totalToSpawn < 0) totalToSpawn = 0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawRangeGizmo(spawnRange1, new Color(0f, 1f, 0f, 0.25f)); // 초록
        DrawRangeGizmo(spawnRange2, new Color(0f, 1f, 1f, 0.25f)); // 청록
    }

    private void DrawRangeGizmo(SpawnRange r, Color c)
    {
        Vector3 size = r.Size;
        if (size.sqrMagnitude <= 0.0001f) return; // 크기 0이면 스킵
        Gizmos.color = c;
        Gizmos.DrawWireCube(r.Center, size);
    }
#endif
}

