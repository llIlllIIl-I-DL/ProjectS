using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// Unity 에디터에서만 작동하는 도구 스크립트
public class ItemPrefabCreator : MonoBehaviour
{
    [MenuItem("Tools/아이템/아이템 프리팹 생성")]
    public static void CreateItemPrefab()
    {
        // 저장 경로 설정
        string savePath = "Assets/Resources/Prefabs";
        
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            string parentFolder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder(parentFolder, "Prefabs");
        }
        
        // 아이템 게임 오브젝트 생성
        GameObject itemObject = new GameObject("Item");
        
        // 컴포넌트 추가
        Rigidbody2D rb = itemObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 중력 영향 없음
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지
        
        BoxCollider2D collider = itemObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);
        
        // 아이템 컴포넌트 추가
        Item itemComponent = itemObject.AddComponent<Item>();
        
        // 스프라이트 렌더러를 위한 자식 오브젝트 추가
        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(itemObject.transform);
        visualObject.transform.localPosition = Vector3.zero;
        
        SpriteRenderer spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // 임시 스프라이트
        spriteRenderer.sortingOrder = 10; // 아이템이 잘 보이도록 Sorting Order 설정
        
        // 프리팹으로 저장
        string prefabPath = $"{savePath}/Item.prefab";
        PrefabUtility.SaveAsPrefabAsset(itemObject, prefabPath);
        
        // 씬에서 임시 오브젝트 제거
        DestroyImmediate(itemObject);
        
        Debug.Log($"아이템 프리팹이 생성되었습니다. 경로: {prefabPath}");
        
        // 프리팹 선택
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
}
#endif 