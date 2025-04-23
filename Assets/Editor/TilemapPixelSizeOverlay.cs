using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Scene 뷰에서 선택된 타일맵 셀의 픽셀 크기를 표시합니다.
[InitializeOnLoad]
public static class TilemapPixelSizeOverlay
{
    static TilemapPixelSizeOverlay()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        // 활성 오브젝트가 Tilemap이 아닐 경우 종료
        if (Selection.activeGameObject == null)
            return;

        var tilemap = Selection.activeGameObject.GetComponent<Tilemap>();
        if (tilemap == null)
            return;

        var grid = tilemap.layoutGrid;
        if (grid == null)
            return;

        // 마우스 월드 좌표 계산
        var evt = Event.current;
        Ray worldRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        Vector3 mouseWorldPos = worldRay.origin;

        // 셀 계산 및 크기
        Vector3Int cell = grid.WorldToCell(mouseWorldPos);
        float cellSize = grid.cellSize.x; // X,Y 동일하다고 가정

        // 현재 셀의 타일을 가져와서 PPU 추출 (없으면 기본값 사용)
        int pixelsPerUnit = 100;
        TileBase tileBase = tilemap.GetTile(cell);
        if (tileBase is Tile tile && tile.sprite != null)
        {
            pixelsPerUnit = Mathf.RoundToInt(tile.sprite.pixelsPerUnit);
        }

        int pixelWidth = Mathf.RoundToInt(cellSize * pixelsPerUnit);
        int pixelHeight = pixelWidth;

        // Scene 뷰에 GUI로 표시
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 200, 40));
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.yellow } };
        GUILayout.Label($"셀 크기: {pixelWidth} px × {pixelHeight} px", style);
        GUILayout.EndArea();
        Handles.EndGUI();

        // 지속적으로 갱신
        sceneView.Repaint();
    }
} 