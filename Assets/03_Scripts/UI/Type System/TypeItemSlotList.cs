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

    static AttributeTypeData currentAttributeType;
    static AttributeTypeData realData;

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
        CurrentPlayersTypeUIUpdate();
    }

    public void CurrentPlayersTypeUIUpdate()
    {
        realData = PlayerUI.Instance.attributeType;
        PlayerUI.Instance.typeName.text = realData.typeName;
        PlayerUI.Instance.typeIcon.sprite = realData.typeIcon; 



        Image[] colorTemp = currentTypePrefab.GetComponentsInChildren<Image>();
        colorTemp[1].color = Color.white;

        Debug.Log($"{colorTemp[0].color}");

        TextMeshProUGUI[] textColor = currentTypePrefab.GetComponentsInChildren<TextMeshProUGUI>();
        textColor[0].color = Color.white;

        Debug.Log($"{colorTemp[0].color}");



    }
}
