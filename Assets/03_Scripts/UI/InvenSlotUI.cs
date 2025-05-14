using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class InvenSlotUI : MonoBehaviour
{
    [Header("슬롯 정보")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNeedPoint;
    [SerializeField] private TextMeshProUGUI itemOwnPoint;
    [SerializeField] private Button slotInteractBtn;

    private ItemData utilityItemData;
    // 필요한 정보를 캐싱해두는 건 좋다
    private Player player; //player의 특성 포인트 현황을 받아오기 위함 

    private void Start()
    {
        player = FindObjectOfType<Player>();


        slotInteractBtn.onClick.AddListener(() => slotInteract());

    }

    public void slotInteract() //포인트 모자랄 때 슬롯 비활성화해주는 메서드도 만들어야 함, 한번 unlock했던 슬롯은 계속 활성화되게!
    {
        if (player.UnLockedUtility.Contains(utilityItemData.id))
        {
            Debug.Log("눌렀습니다!");
            InvenInfoController.Instance.SlotInteract(utilityItemData); //특성 슬롯 눌렀을 시 실행되는 함수.

            return; //함수 종료시키는 거 깜빡하면 안돼!
        }

        Debug.Log("플레이어가 해금한 특성이 아님닙다");
        InvenInfoController.Instance.UnLockedUtility(utilityItemData);
    }


    public void SetItem(ItemData item, Player player)
    {
        utilityItemData = item;
        this.player = player;

        RefreshUI();
    }


    public void RefreshUI()
    {
        if (utilityItemData != null)
        {
            itemName.text = utilityItemData.ItemName;
            itemOwnPoint.text = player.utilityPoint.ToString(); //Player를 받아와야 함
            itemNeedPoint.text = utilityItemData.utilityPointForUnLock.ToString();


            if (player.utilityPoint >= utilityItemData.utilityPointForUnLock)
            {
                itemIcon.sprite = utilityItemData.Icon;
            }

            else
            {
                itemIcon.sprite = utilityItemData.UnLockedIcon;
            }
        }
    }

    public void UpdateOwnPoint() //각 슬롯 내부에 있는 플레이어의 특성 포인트 현황을 업데이트 해주는 함수
    {
        itemOwnPoint.text = player.utilityPoint.ToString();


        if (player.utilityPoint >= utilityItemData.utilityPointForUnLock || player.UnLockedUtility.Contains(utilityItemData.id))
        {
            itemIcon.sprite = utilityItemData.Icon;
        }

        else
        {
            itemIcon.sprite = utilityItemData.UnLockedIcon;
        }


        if (player.utilityPoint >= utilityItemData.utilityPointForUnLock)
        {
            int maxPoint = utilityItemData.utilityPointForUnLock;
            itemOwnPoint.text = maxPoint.ToString();
        }
    }
}
