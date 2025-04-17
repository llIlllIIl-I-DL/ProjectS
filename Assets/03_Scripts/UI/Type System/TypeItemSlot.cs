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

    [SerializeField] public AttributeTypeData attributeTypeData;

    private int slotIndex = 0;


    /*
    public bool IsEmpty()
    {
        return attributeTypeData == null;
    }

    
    public void AddItem(AttributeTypeData attributeTypeData)
    {
        SetItem(attributeTypeData);
        slotIndex++;
    }


    public void SetItem(AttributeTypeData type)
    {
        attributeTypeData = type;
        RefreshUI();
    }


    public void RefreshUI()
    {
        if (attributeTypeData != null)
        {
            typeName.text = attributeTypeData.typeName;
            typeIcon.sprite = attributeTypeData.typeIcon;
        }

        else
        {
            typeName.text = "";
            typeIcon.sprite = null;
        }
    }
    */
}
