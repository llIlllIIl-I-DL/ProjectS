using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InvenSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject invenPopWindow;

    [SerializeField] private Image itemIcon;

    [Header("팝업 창 부분 UI")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private Image iconInPopUp;

    private InvenItemData invenItemData;

    private int slotIndex = 0;



    public void OnPointerEnter(PointerEventData eventData)
    {
        invenPopWindow.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        invenPopWindow.SetActive(false);
    }



    public void AddItem(InvenItemData invenItemData)
    {
        CreatSlotSystem.Instance.slotList[slotIndex].SetItem(invenItemData);
        slotIndex++;
    }


    public void SetItem(InvenItemData item)
    {
        invenItemData = item;
        RefreshUI();
    }


    public void RefreshUI()
    {
        if (invenItemData !=null)
        {
            itemName.text = invenItemData.ItemName;
            itemDescription.text = invenItemData.ItemDescription;

            itemIcon.sprite = invenItemData.Icon;
            iconInPopUp.sprite = invenItemData.Icon;
        }

        else
        {
            itemName.text = "";
            itemDescription.text = "";

            itemIcon.sprite = null;
            iconInPopUp.sprite = null;
        }
    }
}
