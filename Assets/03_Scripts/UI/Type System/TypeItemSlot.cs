using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TypeItemSlot : MonoBehaviour
{
    //처음에는 무조건 normal슬롯이 플레이어의 속성이 됨, 혼자만 ui내에서 활성화.
    //속성 먹었을 때 해당 슬롯 활성화. color.white로...

    //인스펙터는 에디터 상에서
    //start는 런타임에서


    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;
    [SerializeField] public Image slotBackground;

    [SerializeField] public ItemData attributeTypeData;
    
    // 이 슬롯이 나타내는 무기 속성 타입
    [SerializeField] public ElementType slotElementType;

    private int slotIndex = 0;
    private bool isActive = false;

    private void Start()
    {
        // 인벤토리에서 해당 ElementType의 무기 속성 찾기
        FindAttributeTypeFromInventory();
        
        // 초기 상태 설정
        if (slotElementType == ElementType.Normal)
        {
            // Normal 타입은 기본적으로 활성화
            SetActive(true);
        }
        else
        {
            // 다른 타입은 활성화 여부 체크 (인벤토리에 있으면 활성화)
            SetActive(attributeTypeData != null);
        }

        RefreshUI();
    }
    
    // 인벤토리에서 해당 ElementType에 맞는 무기 속성 찾아 할당
    public void FindAttributeTypeFromInventory()
    {
        if (InventoryManager.Instance != null)
        {
            List<ItemData> weaponAttributes = InventoryManager.Instance.GetWeaponAttributes();
            
            // 이미 attributeTypeData가 할당되어 있고 일치하면 유지
            if (attributeTypeData != null && attributeTypeData.elementType == slotElementType)
            {
                return;
            }
            
            // 해당 ElementType의 무기 속성 찾기
            foreach (ItemData item in weaponAttributes)
            {
                if (item.elementType == slotElementType)
                {
                    attributeTypeData = item;
                    SetActive(true);
                    RefreshUI();
                    Debug.Log($"슬롯({slotElementType})에 {item.ItemName} 아이템 자동 할당됨");
                    return;
                }
            }
            
            // 일치하는 아이템을 찾지 못했을 경우
            attributeTypeData = null;
            SetActive(slotElementType == ElementType.Normal); // Normal 타입만 기본 활성화
            RefreshUI();
        }
    }

    public bool IsEmpty()
    {
        return attributeTypeData == null;
    }
    
    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;
        
        // ElementType이 일치하는지 확인
        if (itemData.elementType == slotElementType)
        {
            SetItem(itemData);
            slotIndex++;
        }
    }

    public void SetItem(ItemData item)
    {
        attributeTypeData = item;
        if (item != null)
        {
            slotElementType = item.elementType;
        }
        SetActive(item != null);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (attributeTypeData != null)
        {
            typeName.text = attributeTypeData.ItemName;
            typeIcon.sprite = attributeTypeData.Icon;
        }
        else
        {
            // ElementType에 따라 기본 이름 설정
            typeName.text = slotElementType.ToString() + " 속성";
            typeIcon.sprite = null;
        }
    }

    // 슬롯 활성화/비활성화
    public void SetActive(bool active)
    {
        isActive = active;
        
        // 슬롯 활성화 상태에 따라 색상 변경
        Color textColor = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Color imageColor = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        typeName.color = textColor;
        typeIcon.color = imageColor;
        
        if (slotBackground != null)
        {
            slotBackground.color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    // 인벤토리 매니저에서 호출될 메서드
    public void OnWeaponAttributeEquipped(ItemData equippedAttribute)
    {
        if (equippedAttribute != null)
        {
            // 이 슬롯의 무기 속성이 장착된 무기 속성과 일치하는지 확인
            SetActive(slotElementType == equippedAttribute.elementType);
        }
    }
}
