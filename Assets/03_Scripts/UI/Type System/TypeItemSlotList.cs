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


    public void Start()
    {
        player = FindObjectOfType<Player>();

        currentTypePrefab = TypeAmountList[3];

        temp = TypeAmountList[3].GetComponent<TypeItemSlot>();
        realData = temp.attributeTypeData;

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

    public void CurrentPlayersTypeUIUpdate()
    {
        if (PlayerUI.Instance == null)
        {
            Debug.LogError("PlayerUI.Instance가 null입니다.");
            return;
        }

        realData = PlayerUI.Instance.attributeType;
        
        if (realData == null)
        {
            Debug.LogWarning("PlayerUI.Instance.attributeType이 null입니다.");
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
