using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeItemSlotList : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> TypeAmountList = new List<GameObject>();

    static AttributeTypeData currentAttributeType;

    public void Start()
    {
        currentAttributeType = TypeAmountList[3].gameObject.GetComponent<AttributeTypeData>();


    }

}
