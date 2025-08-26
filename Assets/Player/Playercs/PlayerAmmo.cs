using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAmmo : MonoBehaviour
{
    [Header("Ammo")]
    public int clipSize = 10;     // 탄창 용량(10)
    public int reserveAmmo = 20;  // 예비 탄약(20)
    public int currentAmmo;       // 현재 탄창 탄약

    private PlayerMovement pm;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        currentAmmo = clipSize; // 시작 시 탄창 가득(10)
    }

    public bool HasAmmo() => currentAmmo > 0;

    /// <summary>좌클릭 시 PlayerMovement에서 호출</summary>
    public void Shoot()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            Debug.Log($"발사 → 탄창 {currentAmmo}/{clipSize}, 예비 {reserveAmmo}");
            pm?.PlayAttack(2); // 실사격 모션
        }
        else
        {
            // 자동 재장전 없음: 빈 총 모션만
            Debug.Log("딸깍(빈 탄창). R키로 재장전하세요.");
            pm?.PlayAttack(1); // 빈 총 모션
        }
    }

    /// <summary>R키로만 호출 (자동 재장전 X)</summary>
    public void Reload()
    {
        // 재장전 가능 조건: 예비탄 > 0 && 탄창이 가득 아님
        if (reserveAmmo <= 0 || currentAmmo >= clipSize)
        {
            Debug.Log("재장전 불가 (예비 없음 또는 탄창이 이미 가득).");
            return;
        }

        pm?.PlayReload(); // 재장전 애니메이션 트리거

        // 예) 3/20 → need=7 → take=7 → 10/13
        int need = clipSize - currentAmmo;
        int take = Mathf.Min(need, reserveAmmo);

        currentAmmo += take;
        reserveAmmo -= take;

        Debug.Log($"재장전 완료 → 탄창 {currentAmmo}/{clipSize}, 예비 {reserveAmmo}");
    }
}
