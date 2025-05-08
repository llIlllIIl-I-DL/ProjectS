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

    public Player player;
    public PlayerHP playerHP;

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

    public float changedMaxHP;
    public float actualIncrease;

    public void EquippedUtility(ItemData itemData) //UI 업데이트
    {
        if (player.CurrentUtilityPoint >= itemData.utilityPointForUnLock)
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

                    player.utilityPoint -= itemData.utilityPointForUnLock;
                    PlayerUI.Instance.utilityPointText.text = player.utilityPoint.ToString();

                    player.UpdateCurrentInventory(); //현재는 플레이어 포인트 현황만 업데이트 중
                }
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
        playerHP.DecreaseMaxHP(changedMaxHP, actualIncrease);

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

        WeaponManager.Instance.maxAmmo = Mathf.CeilToInt(previousMaxMP + changedMaxMP);


        // 현재 HP도 최대 HP를 초과하지 않도록 조정

        float previousCurrentMP = WeaponManager.Instance.currentAmmo;
        WeaponManager.Instance.currentAmmo = Mathf.CeilToInt(Mathf.Clamp(WeaponManager.Instance.currentAmmo + actualIncrease, 0, WeaponManager.Instance.maxAmmo));

        Debug.Log($"최대 HP가 {WeaponManager.Instance.maxAmmo - previousMaxMP}만큼 증가했습니다. 새로운 최대 HP: {WeaponManager.Instance.maxAmmo}");

        //player.UpdateCurrentPlayerMP(maxMP); //데이터 저장용
    }

    public void RemovedMaxMPUP()
    {
        Debug.Log("끼얏호~! 1002");
    }




    public void ATKUP(float effectValue, float bulletDamage) //1003
    {
        float nowDamage = bulletDamage;

        if (effectValue <= 0) return; // 0 이하의 값은 무시
        float previousMaxMP = nowDamage; // 이전 최대 MP 저장

        float changedMaxHP = nowDamage * (effectValue / 100);

        WeaponManager.Instance.SetBulletDamage(previousMaxMP + changedMaxHP);

        Debug.Log($"공격력이 {previousMaxMP + changedMaxHP - nowDamage}만큼 증가했습니다. 새로운 공격력: {previousMaxMP + changedMaxHP}");

        //player.UpdateCurrentPlayerATK(bullet.damage); //데이터 저장용
    }

    public void RemovedATKUP()
    {
        Debug.Log("끼얏호~! 1003");
    }





    public void ATKSUP(float effectValue, float bulletSpeed) //1004 이거 weaponManager에 있는 speed로 바꿔야돼!!
    {
        float nowATKSpeed = bulletSpeed;

        if (effectValue <= 0) return; // 0 이하의 값은 무시
        float previouATKsSpeed = nowATKSpeed; // 이전 최대 MP 저장

        float changedATKSpeed = nowATKSpeed * (effectValue / 100);

        WeaponManager.Instance.SetBulletSpeed(previouATKsSpeed + changedATKSpeed);

        Debug.Log($"총알 속도가 {previouATKsSpeed + changedATKSpeed - nowATKSpeed}만큼 증가했습니다. 새로운 총알 속도: {previouATKsSpeed + changedATKSpeed}");

    }
    public void RemovedATKSUP()
    {
        Debug.Log("끼얏호~! 1004");
    }
}
