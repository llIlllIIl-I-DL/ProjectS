using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TypeItemSlot : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;

    private AttributeTypeData attributeTypeData;

    private int slotIndex = 0;

    public void Start()
    {
        attributeTypeData = new AttributeTypeData();
    }

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
}