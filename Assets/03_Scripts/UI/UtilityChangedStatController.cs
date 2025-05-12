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

    public float changedMaxHP;

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
        playerHP.DecreaseMaxHP(changedMaxHP);

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

        WeaponManager.Instance.SetMaxAmmo(Mathf.CeilToInt(previousMaxMP + changedMaxMP));


        // 현재 HP도 최대 HP를 초과하지 않도록 조정

        float previousCurrentMP = WeaponManager.Instance.AmmoManager.CurrentAmmo;
        WeaponManager.Instance.AmmoManager.CurrentAmmo = Mathf.CeilToInt(Mathf.Clamp(WeaponManager.Instance.AmmoManager.CurrentAmmo + actualIncrease, 0, WeaponManager.Instance.AmmoManager.MaxAmmo));

        Debug.Log($"최대 HP가 {WeaponManager.Instance.AmmoManager.MaxAmmo - previousMaxMP}만큼 증가했습니다. 새로운 최대 HP: {WeaponManager.Instance.AmmoManager.MaxAmmo}");

        //player.UpdateCurrentPlayerMP(maxMP); //데이터 저장용
    }

    public void RemovedMaxMPUP()
    {
        Debug.Log("Remove 1002");
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
        }

        Debug.Log($"버프 적용: {effectValue}% 공격력 (현재 총알 속성: {WeaponManager.Instance.GetBulletType()})");
    }

    public void RemovedATKUP()
    {
        Debug.Log("Remove 1003");
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
        }
    }
    public void RemovedATKSUP()
    {
        Debug.Log("Remove 1004");
    }





    public void MSUP(float effectValue)
    {
        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        player.UpdateCurrentPlayerMoveSpeed(percent);
    }

    public void RemovedMSUP()
    {
        Debug.Log("Remove 1005");
    }






    public void RSUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산


        player.UpdateCurrentPlayerRunSpeed(percent);
    }

    public void RemovedRSUP()
    {
        Debug.Log("Remove 1006");
    }




    public void RDUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        player.UpdateCurrentSprintTime(percent);
    }

    public void RemovedRDUP()
    {
        Debug.Log("Remove 1007");
    }




    public void DDUP(float effectValue)
    {

        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        playerSettings.dashDuration += percent;
    }

    public void RemovedDDUP()
    {
        Debug.Log("Remove 1008");
    }




    public void WeighSpeed(float effectValue) 
    {
        if (effectValue <= 0) return;

        float percent = effectValue / 100f; //퍼센트 값 계산

        playerHP.DecreaseMaxHP(effectValue);
        PlayerUI.Instance.UpdatePlayerHPInUItext(); //HP 감소

        player.UpdateCurrentPlayerHP(playerHP.CurrentHP); //데이터 저장


        player.UpdateCurrentPlayerMoveSpeed(percent); //스피드 상승

        ATKSUP(5f); //공속 상승
    }
    public void RemovedWeighSpeed()
    {
        Debug.Log("Remove 1009");
    }




    public void WeighPower(float effectValue) //공증 공속감
    {
        ATKUP(5f);



        ATKSUP(-effectValue); //수정수정
    }
    public void RemovedWeighPower()
    {
        Debug.Log("Remove 1010");
    }




    public void WeighHealth(float effectValue)
    {

    }
    public void RemovedWeighHealth()
    {
        Debug.Log("Remove 1011");
    }



    public void BestDefenceIsAttack(float effectValue)
    {

    }
    public void RemovedBestDefenceIsAttack()
    {
        Debug.Log("Remove 1012");
    }




    public void SpeedRacer(float effectValue)
    {

    }
    public void RemovedSpeedRacer()
    {
        Debug.Log("Remove 1013");
    }




    public void Trinity(float effectValue)
    {

    }
    public void RemovedTrinity()
    {
        Debug.Log("Remove 1014");
    }





    public void InvincibleWhenSprint(float effectValue)
    {

    }
    public void RemovedInvincibleWhenSprint()
    {
        Debug.Log("Remove 1015");
    }

}
