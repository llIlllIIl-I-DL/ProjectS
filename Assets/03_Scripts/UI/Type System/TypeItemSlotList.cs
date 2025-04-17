using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypeItemSlotList : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> TypeAmountList = new List<GameObject>();

    public GameObject currentTypePrefab;

    public AttributeTypeData currentAttributeType;
    public AttributeTypeData realData;

    static Player player;

    TypeItemSlot temp;

    public void Start()
    {
        player = FindObjectOfType<Player>();

        currentTypePrefab = TypeAmountList[3];

        temp = TypeAmountList[3].GetComponent<TypeItemSlot>();
        realData = temp.attributeTypeData;

        player.CurrentattributeTypeData = realData;

        Debug.Log($"{player.CurrentattributeTypeData.typeName}");
        PlayerUI.Instance.CurrentPlayersTypeUIUpdate();
    }
}
