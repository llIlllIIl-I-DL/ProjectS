using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("무기 기본 정보")]
    public string weaponName;
    public Sprite weaponSprite;
    public string weaponDescription;
    
    [Header("무기 속성")]
    public float damage = 1f;
    public float fireRate = 1f;  // 초당 발사 횟수
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    
    [Header("투사체 설정")]
    public GameObject bulletPrefab;  // 무기별 투사체 프리팹
    public float bulletSpeed = 15f;
    public float bulletLifetime = 3f;
    
    [Header("차징 설정")]
    public bool canCharge = true;  // 차징 가능 여부
    public float maxChargeTime = 2.0f;
    public float overchargeThreshold = 1.8f;
    public float chargedDamageMultiplier = 2.0f;
    public float overchargeDamageMultiplier = 3.0f;
    public float chargedSizeMultiplier = 1.5f;
    public float overchargeSizeMultiplier = 2.0f;
    public float overchargePlayerDamagePercent = 5.0f;
} 