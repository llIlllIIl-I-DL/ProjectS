using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class StageMapPreviewWindow : EditorWindow
{
    [MenuItem("Metroidvania/Stage Map Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<StageMapPreviewWindow>("Stage Map Preview");
        window.minSize = new Vector2(400, 300);
    }

    // JSON 데이터용 클래스
    [Serializable]
    public class ConnectionData { public int connectionPointIndex; public string connectedModuleGUID; public int connectedPointIndex; }
    [Serializable]
    public class PositionData { public float x; public float y; }
    [Serializable]
    public class PlacedModuleData
    {
        public string moduleGUID;
        public PositionData position;
        public int rotationStep;
        public List<ConnectionData> connections;
    }
    [Serializable]
    public class StageMapData { public List<PlacedModuleData> placedModules; }

    private StageMapData mapData;
    private Vector2 scrollPos;
    private float previewScale = 1f; // 기본 20 픽셀/유닛으로 설정

    private void OnEnable()
    {
        LoadMapJson();
    }

    private void LoadMapJson()
    {
        var textAsset = Resources.Load<TextAsset>("Maps/Stage_Map");
        if (textAsset != null)
        {
            mapData = JsonUtility.FromJson<StageMapData>(textAsset.text);
        }
        else
        {
            mapData = null;
            Debug.LogError("Stage_Map.json 파일을 Resources/Maps 폴더에서 찾을 수 없습니다.");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Reload JSON", EditorStyles.toolbarButton))
        {
            LoadMapJson();
        }
        // 스케일 슬라이더 (1 ~ 100 픽셀)
        previewScale = EditorGUILayout.Slider("Scale", previewScale, 1f, 100f, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        if (mapData == null || mapData.placedModules == null)
        {
            EditorGUILayout.HelpBox("맵 데이터가 없습니다.", MessageType.Warning);
            return;
        }

        // 기본 실사이즈 적용 (1 모듈 타일 = 10 유니티 타일)
        float unityScale = previewScale * RoomModule.UNITY_TILES_PER_MODULE_TILE;
        // 좌표 범위 계산
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var pm in mapData.placedModules)
        {
            minX = Mathf.Min(minX, pm.position.x);
            minY = Mathf.Min(minY, pm.position.y);
            maxX = Mathf.Max(maxX, pm.position.x);
            maxY = Mathf.Max(maxY, pm.position.y);
        }
        // 모듈이 화면에 잘 보이도록 여유(margin) 추가
        float marginUnits = 1f; // 모듈 여유 공간 (유닛)
        float widthUnits = (maxX - minX) + marginUnits * 2;
        float heightUnits = (maxY - minY) + marginUnits * 2;

        // 스크롤 영역
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        Rect drawArea = GUILayoutUtility.GetRect(widthUnits * unityScale, heightUnits * unityScale);
        // 배경
        EditorGUI.DrawRect(drawArea, new Color(0.1f, 0.1f, 0.1f));

        // 그리드 라인 그리기 (스크롤 영역 안 drawArea 기준)
        Handles.BeginGUI();
        Handles.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        int gridW = Mathf.CeilToInt(widthUnits);
        int gridH = Mathf.CeilToInt(heightUnits);
        // 세로선
        for (int i = 0; i <= gridW; i++)
        {
            float xPos = drawArea.x + i * previewScale;
            Handles.DrawLine(new Vector3(xPos, drawArea.y), new Vector3(xPos, drawArea.y + gridH * previewScale));
        }
        // 가로선
        for (int j = 0; j <= gridH; j++)
        {
            float yPos = drawArea.y + j * previewScale;
            Handles.DrawLine(new Vector3(drawArea.x, yPos), new Vector3(drawArea.x + gridW * previewScale, yPos));
        }
        Handles.EndGUI();

        // 모듈별 블록 그리기
        foreach (var pm in mapData.placedModules)
        {
            // JSON 좌표계 -> 에디터 스크린 좌표계 변환 (마진 적용)
            float x = (pm.position.x - minX + marginUnits) * unityScale + drawArea.x;
            float y = (maxY - pm.position.y + marginUnits) * unityScale + drawArea.y;

            // 로드된 RoomModule ScriptableObject 가져오기
            string assetPath = AssetDatabase.GUIDToAssetPath(pm.moduleGUID);
            RoomModule rm = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            // 모듈 크기 (타일 단위) 가져오기, 없으면 1x1 사용
            Vector2 sizeUnits = (rm != null) ? rm.moduleSize : Vector2.one;
            float w = sizeUnits.x * unityScale;
            float h = sizeUnits.y * unityScale;

            // 블록 영역 계산
            Rect rect = new Rect(x - w * 0.5f, y - h * 0.5f, w, h);

            // 채워진 색상 블록 (GUID 해시 기반) 또는 기본 색
            Color blockColor = (rm != null) ? GetColorFromGUID(pm.moduleGUID) : new Color(0.5f,0.5f,0.5f,0.5f);
            EditorGUI.DrawRect(rect, blockColor);

            // 테두리 표시
            Color borderColor = Color.white;
            // 상단
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderColor);
            // 하단
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), borderColor);
            // 좌측
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), borderColor);
            // 우측
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), borderColor);
        }

        EditorGUILayout.EndScrollView();
    }

    // GUID 문자열을 기반으로 고유 색상 생성
    private Color GetColorFromGUID(string guid)
    {
        int hash = guid.GetHashCode();
        float r = ((hash >> 16) & 0xFF) / 255f;
        float g = ((hash >> 8) & 0xFF) / 255f;
        float b = (hash & 0xFF) / 255f;
        return new Color(r, g, b, 0.8f);
    }
} 