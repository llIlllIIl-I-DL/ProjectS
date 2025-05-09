using System;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    [SerializeField] private int currentAmmo = 30;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private float reloadTime = 1.5f;

    public bool IsReloading { get; private set; }

    // 탄약 변경 시 호출되는 이벤트
    public event Action<int, int> OnAmmoChanged;

    public int CurrentAmmo
    {
        get => currentAmmo;
        set
        {
            currentAmmo = Mathf.Clamp(value, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }
    }
    public int MaxAmmo => maxAmmo;

    public float ReloadTime => reloadTime;

    // 탄약 사용
    public bool UseAmmo()
    {
        if (IsReloading || currentAmmo <= 0)
            return false;

        CurrentAmmo = currentAmmo - 1; // 프로퍼티를 통해 값 감소 및 이벤트 호출

        if (currentAmmo <= 0)
        {
            StartReload();
        }

        return true;
    }

    // 재장전 시작
    public void StartReload()
    {
        if (IsReloading || currentAmmo == maxAmmo)
            return;

        StartCoroutine(ReloadCoroutine());
    }

    // 재장전 처리
    private System.Collections.IEnumerator ReloadCoroutine()
    {
        IsReloading = true;
        Debug.Log("재장전 중...");

        yield return new WaitForSeconds(reloadTime);

        CurrentAmmo = maxAmmo; // 프로퍼티를 통해 값 설정 및 이벤트 호출
        IsReloading = false;
        Debug.Log("재장전 완료!");
    }

    // 속성 변경 메서드
    public void SetMaxAmmo(int value) => maxAmmo = value;
    public void SetReloadTime(float value) => reloadTime = value;
}