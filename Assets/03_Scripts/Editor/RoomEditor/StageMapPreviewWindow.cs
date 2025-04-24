#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class StageMapPreviewWindow : EditorWindow
{
    private GameObject tilemapRoot;
    private GameObject blockPrefab;
    private float blockScale = 1f;
    private GameObject previewContainer;
    private List<Vector3Int> tilePositions;
    private Vector2Int minCell, maxCell;
    private Vector3 cellSize;
    private bool useJson = false;
    private TextAsset jsonFile;

    [MenuItem("Window/Room Editor/Stage Map Preview")]
    public static void ShowWindow()
    {
        GetWindow<StageMapPreviewWindow>("Stage Map Preview");
    }

    private void OnGUI()
    {
        GUILayout.Label("Stage Map Preview", EditorStyles.boldLabel);
        useJson = EditorGUILayout.Toggle("Use JSON File", useJson);
        if (useJson)
        {
            jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        }
        else
        {
            tilemapRoot = (GameObject)EditorGUILayout.ObjectField("Tilemap Root", tilemapRoot, typeof(GameObject), true);
        }
        blockPrefab = (GameObject)EditorGUILayout.ObjectField("Block Prefab", blockPrefab, typeof(GameObject), false);
        blockScale = EditorGUILayout.FloatField("Block Scale", blockScale);

        if (GUILayout.Button("Generate Preview"))
        {
            if (blockPrefab == null)
            {
                Debug.LogError("블록 프리팹이 지정되지 않았습니다.");
            }
            else if (useJson && jsonFile == null)
            {
                Debug.LogError("JSON 파일이 지정되지 않았습니다.");
            }
            else if (!useJson && tilemapRoot == null)
            {
                Debug.LogError("타일맵 루트가 지정되지 않았습니다.");
            }
            else
            {
                if (useJson) GeneratePreviewFromJson();
                else GeneratePreview();
            }
        }
    }

    private void GeneratePreview()
    {
        if (previewContainer != null)
            DestroyImmediate(previewContainer);
        previewContainer = new GameObject("StageMapPreviewContainer");
        previewContainer.hideFlags = HideFlags.HideAndDontSave;
        tilePositions = new List<Vector3Int>();
        GetCombinedTilemapData();

        if (tilePositions.Count == 0)
        {
            Debug.LogWarning("타일이 없습니다.");
            return;
        }

        foreach (var pos in tilePositions)
        {
            Vector3 localPos = new Vector3(pos.x * cellSize.x, pos.y * cellSize.y, 0f);
            GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, previewContainer.transform) as GameObject;
            block.transform.localPosition = localPos;
            block.transform.localScale = Vector3.one * blockScale;
        }
    }

    private void GetCombinedTilemapData()
    {
        Tilemap[] allTilemaps = tilemapRoot.GetComponentsInChildren<Tilemap>();
        if (allTilemaps.Length == 0)
        {
            Debug.LogWarning("타일맵이 발견되지 않았습니다!");
            return;
        }

        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);
        cellSize = allTilemaps[0].cellSize;

        foreach (var tilemap in allTilemaps)
        {
            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.min.x; x < bounds.max.x; x++)
            {
                for (int y = bounds.min.y; y < bounds.max.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cellPos))
                    {
                        tilePositions.Add(cellPos);
                        min.x = Mathf.Min(min.x, x);
                        min.y = Mathf.Min(min.y, y);
                        max.x = Mathf.Max(max.x, x);
                        max.y = Mathf.Max(max.y, y);
                    }
                }
            }
        }

        minCell = new Vector2Int(min.x, min.y);
        maxCell = new Vector2Int(max.x, max.y);
    }

    private void GeneratePreviewFromJson()
    {
        if (previewContainer != null)
            DestroyImmediate(previewContainer);
        previewContainer = new GameObject("StageMapPreviewJsonContainer");
        previewContainer.hideFlags = HideFlags.HideAndDontSave;

        MapJsonData mapData = JsonUtility.FromJson<MapJsonData>(jsonFile.text);
        if (mapData == null || mapData.placedModules == null || mapData.placedModules.Count == 0)
        {
            Debug.LogWarning("JSON에서 placedModules를 찾을 수 없습니다.");
            return;
        }

        foreach (var module in mapData.placedModules)
        {
            var p = module.position;
            Vector3 localPos = new Vector3(p.x, p.y, 0f);
            GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, previewContainer.transform) as GameObject;
            block.transform.localPosition = localPos;
            block.transform.localScale = Vector3.one * blockScale;
        }
    }

    // JSON 데이터 파싱용 클래스
    [Serializable]
    private class MapJsonData
    {
        public List<PlacedModule> placedModules;
    }

    [Serializable]
    private class PlacedModule
    {
        public Position position;
    }

    [Serializable]
    private struct Position
    {
        public float x;
        public float y;
    }

    private void OnDisable()
    {
        if (previewContainer != null)
            DestroyImmediate(previewContainer);
    }
}
#endif
