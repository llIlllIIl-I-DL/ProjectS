using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UtilityChangedStatController : MonoBehaviour
{
    private static UtilityChangedStatController instance;
    public static UtilityChangedStatController Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    static Player player;
    static PlayerHP playerHP;
    [SerializeField] public PlayerSettings playerSettings;


    public float revertMaxHP;
    public float revertMaxMP;
    public float revertATK;
    public float revertATKS;
    public float revertMS;
    public float revertRS;
    public float revertRD;
    public float revertDD;




    public bool isInvincibleDash = false;

    [SerializeField] Bullet bullet;

    private InvenInfoController invenInfoController;

    [Header("장착 중인 특성 아이템 데이터")]
    public List<ItemData> currentUtilityList = new List<ItemData>();//플레이어가 장착한 특성의 리스트
    //UI 갱신은 이 리스트를 통해서....


    private void Start()
    {
        player = FindObjectOfType<Player>();
        playerHP = FindObjectOfType<PlayerHP>();
        invenInfoController = GetComponent<InvenInfoController>();
    }


    public void EquippedUtility(ItemData itemData) //UI 업데이트
    {
        currentUtilityList.Add(itemData);

        for (int i = 0; i < currentUtilityList.Count; i++)
        {
            if (invenInfoController.currentEquippedUtility[i].sprite == null)
            {
                Color temp = invenInfoController.currentEquippedUtility[i].color;
                temp.a = 1f;
                invenInfoController.currentEquippedUtility[i].color = temp;
                invenInfoController.currentEquippedUtility[i].sprite = itemData.Icon;
            }
        }
    }

    public void RemovedUtility(int id) //현재 선택한 슬롯에 할당 된 특성SO의 id값을 갖고온다!
    {
        int removeIndex = currentUtilityList.FindIndex(u => u.id == id); //FindIndex = 특정 값에 일치하는 아이템의 인덱스 리턴
        if (removeIndex < 0) return; //못 찾았을 경우 실행x

        // 데이터 제거
        currentUtilityList.RemoveAt(removeIndex); //해제했기 때문에 currentUtilityList에서 제거!

        // UI 업데이트
        for (int i = 0; i < invenInfoController.currentEquippedUtility.Count; i++) //4번 반복
        {
            Image slot = invenInfoController.currentEquippedUtility[i]; //0번 슬롯 UI(Image) 부터 시작

            if (i < currentUtilityList.Count) //현재 장착 중인 특성 갯수를 계산
            {
                slot.sprite = currentUtilityList[i].Icon; //첫번째 슬롯부터 Icon이미지가 쌓이도록 재배치
                var color = slot.color;
                color.a = 1f;
                slot.color = color;
            }
            else //i > currentUtilityList.Count = 이 앞으로는 빈칸이다!
            {
                slot.sprite = null;
                var color = slot.color;
                color.a = 0f;
                slot.color = color;
            }
        }
    }

    public void MaxHPUP(float effectValue) //1001
    {
        playerHP.IncreaseMaxHP(effectValue);

        PlayerUI.Instance.UpdatePlayerHPInUItext();

        player.UpdateCurrentPlayerHP(playerHP.CurrentHP); //데이터 저장
    }

    public void RemovedMaxHPUP()
    {
        playerHP.DecreaseMaxHP(revertMaxHP);

        PlayerUI.Instance.UpdatePlayerHPInUItext();

        player.UpdateCurrentPlayerHP(playerHP.CurrentHP); //데이터 저장

    }




    public void MaxMPUP(float effectValue, float maxAmmo) //1002
    {
        //maxAmmo = WeaponManager에 있는 에너지 최대치 값

        if (effectValue <= 0) return; // 0 이하의 값은 무시

        float previousMaxMP = maxAmmo; // 이전 최대 HP 저장

        float changedMaxMP = maxAmmo * (effectValue / 100);
        float actualIncrease = previousMaxMP - changedMaxMP; // 최대 HP 증가량

        revertMaxMP = changedMaxMP;

        WeaponManager.Instance.SetMaxAmmo(Mathf.CeilToInt(previousMaxMP + changedMaxMP));

        float previousCurrentMP = WeaponManager.Instance.AmmoManager.CurrentAmmo;
        WeaponManager.Instance.AmmoManager.CurrentAmmo = Mathf.CeilToInt(Mathf.Clamp(WeaponManager.Instance.AmmoManager.CurrentAmmo + actualIncrease, 0, WeaponManager.Instance.AmmoManager.MaxAmmo));

        Debug.Log($"최대 MP가 {WeaponManager.Instance.AmmoManager.MaxAmmo - previousMaxMP}만큼 증가했습니다. 새로운 최대 MP: {WeaponManager.Instance.AmmoManager.MaxAmmo}");
        Debug.Log($"최대 MP: {WeaponManager.Instance.AmmoManager.MaxAmmo} 현재 MP: {WeaponManager.Instance.AmmoManager.CurrentAmmo}");
    }

    public void RemovedMaxMPUP(float maxAmmo)
    {
        float previousMaxMP = maxAmmo; // 이전 최대 HP 저장

        WeaponManager.Instance.SetMaxAmmo(Mathf.FloorToInt(previousMaxMP - revertMaxMP));

        float previousCurrentMP = WeaponManager.Instance.AmmoManager.CurrentAmmo;
        WeaponManager.Instance.AmmoManager.CurrentAmmo = Mathf.CeilToInt(Mathf.Clamp(WeaponManager.Instance.AmmoManager.CurrentAmmo - revertMaxMP, 0, WeaponManager.Instance.AmmoManager.MaxAmmo));

        Debug.Log($"최대 MP: {WeaponManager.Instance.AmmoManager.MaxAmmo} 현재 MP: {WeaponManager.Instance.AmmoManager.CurrentAmmo}");
    }




    public void ATKUP(float effectValue) //1003
    {
        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        WeaponManager.Instance.SetAtkUpPercent(percent); //weaponManager에 전달!!

        // 현재 무기 속성의 기본 데미지로부터 다시 계산
        var type = WeaponManager.Instance.GetBulletType();
        var bulletPrefab = WeaponManager.Instance.BulletFactory.GetBulletPrefab(type);
        var bullet = bulletPrefab.GetComponent<Bullet>();
        if (bullet != null)
        {
            float newDamage = bullet.Damage * (1f + WeaponManager.Instance.AtkUpPercent);

            WeaponManager.Instance.SetBulletDamage(newDamage);

            revertATK = newDamage;
        }

        Debug.Log($"버프 적용: {effectValue}% 공격력 (현재 총알 속성: {WeaponManager.Instance.GetBulletType()})");
    }

    public void RemovedATKUP()
    {
        WeaponManager.Instance.SetAtkUpPercent(0); //weaponManager에 전달!!

        var type = WeaponManager.Instance.GetBulletType();
        var bulletPrefab = WeaponManager.Instance.BulletFactory.GetBulletPrefab(type);
        var bullet = bulletPrefab.GetComponent<Bullet>();

        if (bullet != null)
        {
            float newDamage = revertATK - (revertATK - 1f);

            WeaponManager.Instance.SetBulletDamage(newDamage);
        }
    }




    public void ATKSUP(float effectValue)
    {
        //if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        WeaponManager.Instance.SetSpeedUpPercent(percent); //weaponManager에 전달!!

        // 현재 무기 속성의 기본 데미지로부터 다시 계산
        var type = WeaponManager.Instance.GetBulletType();
        var bulletPrefab = WeaponManager.Instance.BulletFactory.GetBulletPrefab(type);
        var bullet = bulletPrefab.GetComponent<Bullet>();

        if (bullet != null)
        {
            float newDamage = bullet.BulletSpeed * (1f + WeaponManager.Instance.SpeedUpPercent);
            WeaponManager.Instance.SetBulletSpeed(newDamage);
            revertATKS = newDamage;
        }
    }

    public void RemovedATKSUP()
    {
        WeaponManager.Instance.SetSpeedUpPercent(0); //weaponManager에 전달!!

        var type = WeaponManager.Instance.GetBulletType();
        var bulletPrefab = WeaponManager.Instance.BulletFactory.GetBulletPrefab(type);
        var bullet = bulletPrefab.GetComponent<Bullet>();

        if (bullet != null)
        {
            float newDamage = revertATKS - (revertATKS - 1f);

            WeaponManager.Instance.SetBulletSpeed(newDamage);
        }
    }





    public void MSUP(float effectValue)
    {
        //if (effectValue <= 0) return;

        float percent = effectValue / 100f;

        float moveSpeed = playerSettings.moveSpeed * percent; //퍼센트 값 계산

        player.UpdateCurrentPlayerMoveSpeed(moveSpeed);

        revertMS = moveSpeed;

    }

    public void RemovedMSUP()
    {
        player.UpdateCurrentPlayerMoveSpeed(-revertMS);
    }






    public void RSUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        float sprintSpeed = playerSettings.sprintMultiplier * percent;

        player.UpdateCurrentPlayerRunSpeed(sprintSpeed);

        revertRS = sprintSpeed;
    }

    public void RemovedRSUP()
    {
        player.UpdateCurrentPlayerRunSpeed(-revertRS);
        
    }




    public void RDUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        float sprintDuration = player.SprintDuration * percent;

        player.UpdateCurrentSprintTime(sprintDuration);

        revertRD = sprintDuration;
    }

    public void RemovedRDUP()
    {
        player.UpdateCurrentSprintTime(-revertRD);
    }




    public void DDUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        float dashDuration = playerSettings.dashDuration * percent;

        playerSettings.dashDuration += dashDuration;

        revertDD = dashDuration;
    }

    public void RemovedDDUP()
    {
        playerSettings.dashDuration -= revertDD;
    }




    public void WeighSpeed(float effectValue) 
    {
        if (effectValue <= 0) return;

        playerHP.DecreaseMaxHP(effectValue);
        PlayerUI.Instance.UpdatePlayerHPInUItext(); //HP 감소

        player.UpdateCurrentPlayerMoveSpeed(effectValue); //스피드 상승

        ATKSUP(5f); //공속 상승
    }

    public void RemovedWeighSpeed(float effectValue)
    {
        MaxHPUP(effectValue);

        RemovedMSUP(); //스피드 감소

        ATKSUP(-5f); //공속 감소
    }




    public void WeighPower(float effectValue) //공증 공속감
    {
        ATKUP(5f);

        ATKSUP(-effectValue); //수정수정
    }

    public void RemovedWeighPower()
    {
        RemovedATKUP();

        RemovedATKSUP();
    }




    public void WeighHealth(float effectValue)
    {
        MaxHPUP(effectValue);

        MSUP(-3f);
    }

    public void RemovedWeighHealth()
    {
        RemovedMaxHPUP();

        RemovedMSUP();
    }



    public void BestDefenceIsAttack(float effectValue)
    {
        float decreaseHP = effectValue * 3;
        playerHP.DecreaseMaxHP(decreaseHP); //최대 hp 감소

        float increaseATK = effectValue * 2;
        ATKUP(increaseATK); //ATK 상승

        ATKSUP(increaseATK); //ATKS 상승

        MSUP(-effectValue); //MS 감소

        RSUP(effectValue); // RS 상승
    }

    public void RemovedBestDefenceIsAttack(float effectValue)
    {
        float IncreaseHP = effectValue * 3;
        MaxHPUP(IncreaseHP); //최대 hp 상승

        float decreaseATK = effectValue * 2;
        RemovedATKUP(); //ATK 감소

        RemovedATKSUP(); //ATKS 감소

        MSUP(effectValue); //MS 상승

        RemovedRSUP(); // RS 감소
    }




    public void SpeedRacer(float effectValue)
    {
        float decreaseHP = effectValue * 3;
        playerHP.DecreaseMaxHP(decreaseHP); //최대 hp 감소

        ATKUP(- effectValue); //ATK 감소

        ATKSUP(decreaseHP); //ATKS 상승

        MSUP(decreaseHP); //MS 상승

        RDUP(3f); //RD 상승
    }

    public void RemovedSpeedRacer(float effectValue)
    {
        float increaseHP = effectValue * 3;

        MaxHPUP(increaseHP);

        RemovedATKUP();

        RemovedATKSUP();

        RemovedMSUP();

        RemovedRDUP();
    }




    public void Trinity(float effectValue, float maxAmmo)
    {
        MaxHPUP(effectValue); //최대 hp 상승

        MaxMPUP(effectValue, maxAmmo); //최대 MP 상승

        ATKUP(effectValue); //ATK 상승

        ATKSUP(effectValue); //ATKS 상승

        MSUP(effectValue); //MS 상승

        RSUP(effectValue); //RS 상승

        RDUP(effectValue); //RD 상승

        DDUP(effectValue); //DD상승

    }
    public void RemovedTrinity(float maxAmmo)
    {
        RemovedMaxHPUP();

        RemovedMaxMPUP(maxAmmo);

        RemovedATKUP();

        RemovedATKSUP();

        RemovedMSUP();

        RemovedRSUP();

        RemovedRDUP();

        RemovedDDUP();
    }





    public void InvincibleWhenDash()
    {
        isInvincibleDash = true;
    }
    public void RemovedInvincibleWhenDash()
    {
        isInvincibleDash = false;
    }

}
