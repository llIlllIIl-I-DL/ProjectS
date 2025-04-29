using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class UtilityChangedStatController : MonoBehaviour
{

    public Player player;
    public PlayerHP playerHP;

    [SerializeField] Bullet bullet;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        playerHP = FindObjectOfType<PlayerHP>();
    }




    //해제는 어떻게 구현하면 좋을까...>>변화 값 지역 변수로 저장 후 해제 시 원본 값에서 변수값 제외하기!!

    public void MaxHPUP(float effectValue) //1001
    {
        playerHP.IncreaseMaxHP(effectValue);
        PlayerUI.Instance.UpdatePlayerHPInUItext();

        player.UpdateCurrentPlayerHP(playerHP.MaxHP); //데이터 저장
    }

    public void MaxMPUP(float effectValue, float maxAmmo) //1002
    {
        //maxAmmo = WeaponManager에 있는 에너지 최대치 값


        if (effectValue <= 0) return; // 0 이하의 값은 무시
        float previousMaxMP = maxAmmo; // 이전 최대 HP 저장

        float changedMaxMP = maxAmmo * (effectValue / 100);
        float actualIncrease = previousMaxMP - changedMaxMP; // 최대 HP 증가량

        WeaponManager.Instance.maxAmmo = Mathf.CeilToInt(previousMaxMP + changedMaxMP);


        // 현재 HP도 최대 HP를 초과하지 않도록 조정

        float previousCurrentMP = WeaponManager.Instance.currentAmmo;
        WeaponManager.Instance.currentAmmo = Mathf.CeilToInt(Mathf.Clamp(WeaponManager.Instance.currentAmmo + actualIncrease, 0, WeaponManager.Instance.maxAmmo));

        Debug.Log($"최대 HP가 {WeaponManager.Instance.maxAmmo - previousMaxMP}만큼 증가했습니다. 새로운 최대 HP: {WeaponManager.Instance.maxAmmo}");

        //player.UpdateCurrentPlayerMP(maxMP); //데이터 저장용
    }

    public void ATKUP(float effectValue, float bulletDamage) //1003
    {
        float nowDamage = bulletDamage;

        if (effectValue <= 0) return; // 0 이하의 값은 무시
        float previousMaxMP = nowDamage; // 이전 최대 MP 저장

        float changedMaxHP = nowDamage * (effectValue / 100);

        bullet.damage = previousMaxMP + changedMaxHP;

        Debug.Log($"최대 HP가 {bullet.damage - nowDamage}만큼 증가했습니다. 새로운 최대 HP: {bullet.damage}");

        //player.UpdateCurrentPlayerATK(bullet.damage); //데이터 저장용
    }

    public void ATKSUP(float effectValue, float bulletSpeed) //1004
    {
        float nowATKSpeed = bulletSpeed;

        if (effectValue <= 0) return; // 0 이하의 값은 무시
        float previouATKsSpeed = nowATKSpeed; // 이전 최대 MP 저장

        float changedATKSpeed = nowATKSpeed * (effectValue / 100);

        bullet.bulletSpeed = previouATKsSpeed + changedATKSpeed;

        Debug.Log($"최대 HP가 {bullet.bulletSpeed - nowATKSpeed}만큼 증가했습니다. 새로운 최대 HP: {bullet.bulletSpeed}");

    }
}
