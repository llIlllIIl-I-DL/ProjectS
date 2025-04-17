using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TypeItem : MonoBehaviour
{
    [SerializeField] AttributeTypeData attributeTypeData;
    static Sprite iconSprite;
    
    static SpriteRenderer iconSpriteRenderer;

    public void Start()
    {
        iconSpriteRenderer = GetComponent<SpriteRenderer>();

        iconSprite = attributeTypeData.typeIcon;
        iconSpriteRenderer.sprite = iconSprite;
    }

    private void OnTriggerEnter2D()
    {
       CollectTypeItem();
    }

    public void CollectTypeItem()
    {
        PlayerUI.Instance.TypeItemDic.Add(attributeTypeData, iconSprite);

        Debug.Log("키에엑");

        Destroy(gameObject);
    }
}
