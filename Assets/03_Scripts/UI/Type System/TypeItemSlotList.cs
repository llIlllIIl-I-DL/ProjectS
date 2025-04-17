using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypeItemSlotList : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> TypeAmountList = new List<GameObject>();

    static GameObject currentTypePrefab;

    static ItemData currentAttributeType;
    static ItemData realData;

    static Player player;

    TypeItemSlot temp;
    private List<TypeItemSlot> typeSlots = new List<TypeItemSlot>();

    public void Start()
    {
        player = FindObjectOfType<Player>();

        // 모든 타입 슬롯 참조 저장
        foreach (GameObject slotObj in TypeAmountList)
        {
            TypeItemSlot slot = slotObj.GetComponent<TypeItemSlot>();
            if (slot != null)
            {
                typeSlots.Add(slot);
            }
        }

        currentTypePrefab = TypeAmountList[3];

        temp = TypeAmountList[3].GetComponent<TypeItemSlot>();
        realData = temp.attributeTypeData;

        // 인벤토리 매니저 이벤트 구독
        if (InventoryManager.Instance != null)
        {
            // 무기 장착 이벤트 구독
            InventoryManager.Instance.OnItemEquipped += OnWeaponAttributeChanged;
            
            // 아이템 추가 이벤트 구독 - 새 무기 속성 추가 시 UI 업데이트
            InventoryManager.Instance.OnItemAdded += OnItemAdded;
            
            // 인벤토리에 있는 모든 무기 속성 아이템에 대해 슬롯 활성화
            UpdateAllWeaponAttributeSlots();
            
            // 초기 장착된 무기 속성 가져오기
            ItemData equippedWeapon = InventoryManager.Instance.GetCurrentWeaponAttribute();
            if (equippedWeapon != null)
            {
                OnWeaponAttributeChanged(equippedWeapon);
            }
        }
        else
        {
            Debug.LogWarning("InventoryManager.Instance가 null입니다.");
        }

        if (player != null)
        {
            player.CurrentattributeTypeData = realData;

            if (player.CurrentattributeTypeData != null)
            {
                Debug.Log($"{player.CurrentattributeTypeData.ItemName}");
            }
            else
            {
                Debug.LogWarning("CurrentattributeTypeData가 null입니다.");
            }
        }
        else
        {
            Debug.LogError("Player를 찾을 수 없습니다.");
            return;
        }
        
        CurrentPlayersTypeUIUpdate();
    }

    // 인벤토리에 있는 모든 무기 속성에 대해 슬롯 활성화
    private void UpdateAllWeaponAttributeSlots()
    {
        if (InventoryManager.Instance != null)
        {
            List<ItemData> weaponAttributes = InventoryManager.Instance.GetWeaponAttributes();
            
            // 모든 무기 속성에 대해 처리
            foreach (ItemData weaponAttribute in weaponAttributes)
            {
                // 해당 ElementType의 슬롯에 자동 할당
                AssignItemToMatchingSlot(weaponAttribute);
                
                // 슬롯 활성화 처리
                ActivateWeaponAttributeSlot(weaponAttribute);
            }
            
            // 인벤토리에 없는 속성 슬롯들은 각자 처리하도록
            foreach (TypeItemSlot slot in typeSlots)
            {
                slot.FindAttributeTypeFromInventory();
            }
        }
    }

    // 아이템 추가 시 호출되는 이벤트 핸들러
    private void OnItemAdded(ItemData item)
    {
        // 추가된 아이템이 무기 속성인 경우에만 처리
        if (item != null && item.itemType == ItemType.WeaponAttribute)
        {
            // 해당 ElementType의 슬롯에 아이템 자동 할당
            AssignItemToMatchingSlot(item);
            
            // 슬롯 활성화 처리
            ActivateWeaponAttributeSlot(item);
        }
    }

    // 무기 속성 아이템을 해당 ElementType과 일치하는 슬롯에 할당
    private void AssignItemToMatchingSlot(ItemData weaponAttribute)
    {
        if (weaponAttribute == null) return;
        
        foreach (TypeItemSlot slot in typeSlots)
        {
            // 슬롯의 ElementType과 아이템의 ElementType이 일치하면 자동 할당
            if (slot.slotElementType == weaponAttribute.elementType)
            {
                // 슬롯의 attributeTypeData에 해당 아이템 데이터 설정
                slot.SetItem(weaponAttribute);
                Debug.Log($"슬롯에 {weaponAttribute.ItemName} 속성 자동 할당됨");
                break;
            }
        }
    }

    // 해당 무기 속성에 대한 슬롯 활성화
    private void ActivateWeaponAttributeSlot(ItemData weaponAttribute)
    {
        if (weaponAttribute == null) return;
        
        foreach (TypeItemSlot slot in typeSlots)
        {
            // 슬롯의 무기 속성과 추가된 무기 속성이 일치하는 경우
            if (slot.attributeTypeData != null && 
                slot.attributeTypeData.elementType == weaponAttribute.elementType)
            {
                // 슬롯에 아이템 추가 및 활성화
                slot.AddItem(weaponAttribute);
                break;
            }
        }
    }

    // 무기 속성 변경 시 호출되는 이벤트 핸들러
    private void OnWeaponAttributeChanged(ItemData item)
    {
        // 무기 속성 아이템인 경우에만 처리
        if (item != null && item.itemType == ItemType.WeaponAttribute)
        {
            // 현재 장착된 무기 속성 업데이트
            currentAttributeType = item;
            
            // 모든 슬롯의 상태 업데이트
            foreach (TypeItemSlot slot in typeSlots)
            {
                slot.OnWeaponAttributeEquipped(item);
                
                // 현재 장착된 속성과 일치하는 슬롯을 현재 선택된 슬롯으로 설정
                if (slot.attributeTypeData != null && 
                    slot.attributeTypeData.elementType == item.elementType)
                {
                    // 현재 선택된 타입 슬롯 업데이트
                    currentTypePrefab = slot.gameObject;
                    realData = slot.attributeTypeData;
                    
                    // 플레이어 속성 업데이트
                    if (player != null)
                    {
                        player.CurrentattributeTypeData = realData;
                    }
                }
            }
            
            // UI 업데이트
            CurrentPlayersTypeUIUpdate();
        }
    }

    public void CurrentPlayersTypeUIUpdate()
    {
        if (PlayerUI.Instance == null)
        {
            Debug.LogError("PlayerUI.Instance가 null입니다.");
            return;
        }

        realData = PlayerUI.Instance.attributeType;
        
        // 현재 장착된 무기 속성이 있으면 그것을 사용
        if (currentAttributeType != null)
        {
            realData = currentAttributeType;
            PlayerUI.Instance.attributeType = currentAttributeType;
        }
        
        if (realData == null)
        {
            Debug.LogWarning("속성 데이터가 null입니다.");
            return;
        }
        
        PlayerUI.Instance.typeName.text = realData.ItemName;
        PlayerUI.Instance.typeIcon.sprite = realData.Icon; 

        if (currentTypePrefab == null)
        {
            Debug.LogError("currentTypePrefab이 null입니다.");
            return;
        }

        Image[] colorTemp = currentTypePrefab.GetComponentsInChildren<Image>();
        
        if (colorTemp == null || colorTemp.Length < 2)
        {
            Debug.LogError("currentTypePrefab에서 Image 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        colorTemp[1].color = Color.white;

        Debug.Log($"{colorTemp[0].color}");

        TextMeshProUGUI[] textColor = currentTypePrefab.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (textColor == null || textColor.Length < 1)
        {
            Debug.LogError("currentTypePrefab에서 TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        textColor[0].color = Color.white;

        Debug.Log($"{colorTemp[0].color}");
    }
}
