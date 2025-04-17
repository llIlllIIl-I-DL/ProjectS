// 3. 복장 세트 데이터 클래스 추가
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Costume Set", menuName = "ScriptableObjects/CostumeSet")]
public class CostumeSetData : ScriptableObject
{
    public string costumeId;
    public string costumeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("필요한 파츠")]
    public List<ItemData> requiredParts = new List<ItemData>();

    [Header("특수 능력")]
    public string abilityName;
    [TextArea(2, 5)]
    public string abilityDescription;

    // 런타임에 확인할 프로퍼티
    [HideInInspector]
    public bool isUnlocked;
}