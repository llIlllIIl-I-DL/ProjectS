using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// Unity 에디터에서만 작동하는 도구 스크립트
public class ItemDemoCreator : MonoBehaviour
{
    // 아이템 생성 메뉴 추가
    [MenuItem("Tools/아이템/데모 아이템 생성")]
    public static void CreateDemoItems()
    {
        // 저장 경로 설정
        string savePath = "Assets/Resources/Items";
        
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            string parentFolder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder(parentFolder, "Items");
        }
        
        // 회복 아이템 생성
        CreateHealItem(savePath);
        
        // 최대 체력 증가 아이템 생성
        CreateMaxHPUpItem(savePath);
        
        // 에셋 데이터베이스 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("데모 아이템 생성 완료!");
    }
    
    private static void CreateHealItem(string path)
    {
        // 소형 체력 회복 아이템
        ItemData smallHealItem = ScriptableObject.CreateInstance<ItemData>();
        smallHealItem.ItemName = "작은 에너지 팩";
        smallHealItem.itemAttributeType = ItemAttributeType.HealItem;
        smallHealItem.ItemDescription = "체력을 20만큼 회복합니다.";
        smallHealItem.effectValue = 20f;
        smallHealItem.isConsumable = true;
        smallHealItem.isStackable = true;
        smallHealItem.maxStackAmount = 5;
        
        AssetDatabase.CreateAsset(smallHealItem, $"{path}/SmallHealItem.asset");
        
        // 대형 체력 회복 아이템
        ItemData largeHealItem = ScriptableObject.CreateInstance<ItemData>();
        largeHealItem.ItemName = "큰 에너지 팩";
        largeHealItem.itemAttributeType = ItemAttributeType.HealItem;
        largeHealItem.ItemDescription = "체력을 50만큼 회복합니다.";
        largeHealItem.effectValue = 50f;
        largeHealItem.isConsumable = true;
        largeHealItem.isStackable = true;
        largeHealItem.maxStackAmount = 3;
        
        AssetDatabase.CreateAsset(largeHealItem, $"{path}/LargeHealItem.asset");
    }
    
    private static void CreateMaxHPUpItem(string path)
    {
        // 소형 최대 체력 증가 아이템
        ItemData smallMaxHPUpItem = ScriptableObject.CreateInstance<ItemData>();
        smallMaxHPUpItem.ItemName = "에너지 탱크";
        smallMaxHPUpItem.itemAttributeType = ItemAttributeType.MaxHPUpItem;
        smallMaxHPUpItem.ItemDescription = "최대 체력을 10만큼 증가시킵니다.";
        smallMaxHPUpItem.effectValue = 10f;
        smallMaxHPUpItem.isConsumable = true;
        smallMaxHPUpItem.isStackable = false;
        
        AssetDatabase.CreateAsset(smallMaxHPUpItem, $"{path}/SmallMaxHPUpItem.asset");
        
        // 대형 최대 체력 증가 아이템
        ItemData largeMaxHPUpItem = ScriptableObject.CreateInstance<ItemData>();
        largeMaxHPUpItem.ItemName = "생명력 탱크";
        largeMaxHPUpItem.itemAttributeType = ItemAttributeType.MaxHPUpItem;
        largeMaxHPUpItem.ItemDescription = "최대 체력을 25만큼 증가시킵니다.";
        largeMaxHPUpItem.effectValue = 25f;
        largeMaxHPUpItem.isConsumable = true;
        largeMaxHPUpItem.isStackable = false;
        
        AssetDatabase.CreateAsset(largeMaxHPUpItem, $"{path}/LargeMaxHPUpItem.asset");
    }
}
#endif 