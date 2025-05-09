using System;
using UnityEngine;

public class ChargeManager : MonoBehaviour
{
    [Header("차징 설정")]
    [SerializeField] private float chargingLevel1Time = 1.0f; // 1단계 차지에 필요한 시간
    [SerializeField] private float chargingLevel2Time = 2.5f; // 2단계 차지에 필요한 시간

    [Header("차징 상태")]
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private int currentChargeLevel = 0;

    // 차징 상태 및 레벨을 외부에서 확인할 수 있는 프로퍼티
    public bool IsCharging => isCharging;
    public float CurrentChargeTime => currentChargeTime;
    public int CurrentChargeLevel => currentChargeLevel;

    // 차징 레벨 변경 이벤트
    public event Action<int> OnChargeLevelChanged;
    public event Action<float> OnChargePressureChanged;

    // 차징 시작
    public void StartCharging()
    {
        isCharging = true;
        currentChargeTime = 0f;
        currentChargeLevel = 0;
        Debug.Log("압력 충전 시작");

        // 초기 압력 변경 이벤트 발생
        OnChargePressureChanged?.Invoke(0f);
    }

    // 차징 업데이트 (매 프레임마다 호출)
    public void UpdateCharging()
    {
        if (!isCharging) return;

        float previousChargeTime = currentChargeTime;
        currentChargeTime += Time.deltaTime;

        // 이전 레벨과 현재 레벨 확인하여 변화가 있으면 이벤트 발생
        int previousLevel = currentChargeLevel;

        // 차징 레벨 결정
        if (currentChargeTime >= chargingLevel2Time)
        {
            currentChargeLevel = 2;
        }
        else if (currentChargeTime >= chargingLevel1Time)
        {
            currentChargeLevel = 1;
        }
        else
        {
            currentChargeLevel = 0;
        }

        // 레벨이 변경되었을 때 이벤트 발생
        if (previousLevel != currentChargeLevel)
        {
            OnChargeLevelChanged?.Invoke(currentChargeLevel);
        }

        // 현재 충전 시간에 비례하여 압력 설정 (0-1 사이 값)
        float pressure = Mathf.Clamp01(currentChargeTime / chargingLevel2Time);
        OnChargePressureChanged?.Invoke(pressure);
    }

    // 차징 중단
    public void StopCharging()
    {
        isCharging = false;
    }

    // 차징 완전 리셋 (재장전 등으로 초기화 필요할 때)
    public void ResetCharging()
    {
        isCharging = false;
        currentChargeTime = 0f;
        currentChargeLevel = 0;
        OnChargePressureChanged?.Invoke(0f);
    }

    // 속성 변경 메서드
    public void SetLevel1Time(float value) => chargingLevel1Time = value;
    public void SetLevel2Time(float value) => chargingLevel2Time = value;
}