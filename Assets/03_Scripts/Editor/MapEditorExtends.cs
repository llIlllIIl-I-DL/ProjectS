using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExtendedMapEditorWindow : EditorWindow
{
    #region Editor Settings
    // 에디터 설정
    private List<TilesetData> tilesets = new List<TilesetData>();
    private int selectedTilesetIndex = 0;
    private int selectedTileIndex = 0;

    // 레이어 시스템
    private List<LayerData> layers = new List<LayerData>();
    private int activeLayerIndex = 0;
    private Vector2 layerScrollPosition;

    // 브러시 설정
    private enum BrushType
    {
        Single,
        Rectangle,
        Fill
    }
    private BrushType currentBrushType = BrushType.Single;
    private Vector2Int brushSize = new Vector2Int(1, 1);
    private bool randomizeTiles = false;

    // 타일 변형
    public enum TileVariant
    {
        Normal,
        FlipX,
        FlipY,
        Rotate90,
        Rotate180,
        Rotate270
    }
    private TileVariant currentTileVariant = TileVariant.Normal;

    // 에디터 상태
    private enum PlacementMode
    {
        Tile,
        Platform,
        Enemy,
        Collectible,
        Eraser,
        Select
    }
    private PlacementMode currentMode = PlacementMode.Tile;

    // 선택 영역
    private bool isSelecting = false;
    private Vector2 selectionStart;
    private Vector2 selectionEnd;
    private List<MapObject> selectedObjects = new List<MapObject>();

    // 스냅 설정
    private float snapValue = 1f;
    private bool snapToGrid = true;

    // 맵 데이터
    private List<MapObject> mapObjects = new List<MapObject>();
    private string mapName = "NewMap";

    // 타일셋 미리보기
    private Vector2 tilesetScrollPosition;
    private float tilesetPreviewSize = 64f;
    private Texture2D gridTexture;

    // 언두/리두 시스템
    private List<UndoRedoAction> undoActions = new List<UndoRedoAction>();
    private List<UndoRedoAction> redoActions = new List<UndoRedoAction>();
    private int maxUndoSteps = 50;

    // 미리보기 설정
    private bool showPreview = true;
    private Color previewTint = new Color(1, 1, 1, 0.5f);
    private GameObject previewInstance;

    // 에디터 스타일
    private GUIStyle boldLabel;
    private GUIStyle boxStyle;
    private GUIStyle activeLayerStyle;
    private GUIStyle inactiveLayerStyle;
    #endregion

    #region Initialization
    [MenuItem("Window/2D Platformer Map Editor (Extended)")]
    public static void ShowWindow()
    {
        GetWindow<ExtendedMapEditorWindow>("확장 맵 에디터");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // 기본 레이어 생성
        if (layers.Count == 0)
        {
            layers.Add(new LayerData { Name = "Background", Visible = true, Locked = false });
            layers.Add(new LayerData { Name = "Terrain", Visible = true, Locked = false });
            layers.Add(new LayerData { Name = "Objects", Visible = true, Locked = false });
            layers.Add(new LayerData { Name = "Foreground", Visible = true, Locked = false });
            activeLayerIndex = 1; // Terrain 레이어를 기본 활성화
        }

        // 그리드 텍스처 생성
        CreateGridTexture();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        // 미리보기 삭제
        DestroyPreview();
    }

    private void CreateGridTexture()
    {
        int size = 64;
        gridTexture = new Texture2D(size, size);

        // 배경을 투명하게 설정
        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }

        // 그리드 라인 그리기
        for (int x = 0; x < size; x++)
        {
            colors[x] = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors[x + size * (size - 1)] = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        for (int y = 0; y < size; y++)
        {
            colors[y * size] = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors[(y * size) + size - 1] = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        gridTexture.SetPixels(colors);
        gridTexture.Apply();
    }
    #endregion

    #region GUI
    private void OnGUI()
    {
        InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        // 왼쪽 패널 (타일셋, 레이어)
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        DrawTilesetPanel();
        DrawLayerPanel();
        EditorGUILayout.EndVertical();

        // 오른쪽 패널 (도구, 설정)
        EditorGUILayout.BeginVertical();
        DrawToolPanel();
        DrawSettingsPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // 하단 상태 바 및 버튼
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("언두", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            Undo();
        }

        if (GUILayout.Button("리두", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            Redo();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Label($"오브젝트 수: {mapObjects.Count}", EditorStyles.miniLabel);
        GUILayout.Label($"선택됨: {selectedObjects.Count}", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();

        ProcessKeyboardShortcuts();
    }

    private void InitializeStyles()
    {
        if (boldLabel == null)
        {
            boldLabel = new GUIStyle(EditorStyles.boldLabel);
            boldLabel.fontSize = 12;
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        if (activeLayerStyle == null)
        {
            activeLayerStyle = new GUIStyle(EditorStyles.miniButton);
            activeLayerStyle.normal.background = MakeTex(2, 2, new Color(0.0f, 0.5f, 0.8f, 0.5f));
            activeLayerStyle.fontStyle = FontStyle.Bold;
        }

        if (inactiveLayerStyle == null)
        {
            inactiveLayerStyle = new GUIStyle(EditorStyles.miniButton);
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawTilesetPanel()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("타일셋", boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("타일셋 추가", GUILayout.Width(100)))
        {
            AddTileset();
        }

        if (GUILayout.Button("타일셋 제거", GUILayout.Width(100)))
        {
            if (tilesets.Count > 0 && EditorUtility.DisplayDialog("타일셋 제거", "선택한 타일셋을 제거하시겠습니까?", "예", "아니오"))
            {
                RemoveTileset(selectedTilesetIndex);
            }
        }
        EditorGUILayout.EndHorizontal();

        // 타일셋 선택
        if (tilesets.Count > 0)
        {
            string[] tilesetNames = tilesets.Select(t => t.Name).ToArray();
            selectedTilesetIndex = EditorGUILayout.Popup("타일셋 선택", selectedTilesetIndex, tilesetNames);

            // 타일셋 미리보기
            if (selectedTilesetIndex < tilesets.Count)
            {
                TilesetData selectedTileset = tilesets[selectedTilesetIndex];

                // 타일 크기 조절
                tilesetPreviewSize = EditorGUILayout.Slider("타일 크기", tilesetPreviewSize, 32f, 128f);

                // 스크롤 영역 시작
                tilesetScrollPosition = EditorGUILayout.BeginScrollView(tilesetScrollPosition,
                    GUILayout.Height(200));

                // 타일셋 그리드 계산
                if (selectedTileset.Tiles.Count > 0)
                {
                    int columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 60) / tilesetPreviewSize));
                    int rows = Mathf.CeilToInt((float)selectedTileset.Tiles.Count / columns);

                    // 타일셋 그리드 배경
                    Rect totalRect = GUILayoutUtility.GetRect(columns * tilesetPreviewSize,
                        rows * tilesetPreviewSize);

                    // 타일 표시
                    for (int i = 0; i < selectedTileset.Tiles.Count; i++)
                    {
                        int row = i / columns;
                        int col = i % columns;

                        Rect tileRect = new Rect(totalRect.x + col * tilesetPreviewSize,
                            totalRect.y + row * tilesetPreviewSize,
                            tilesetPreviewSize, tilesetPreviewSize);

                        // 타일 배경 (그리드)
                        GUI.DrawTexture(tileRect, gridTexture);

                        // 타일 프리뷰
                        TileData tile = selectedTileset.Tiles[i];
                        if (tile.Prefab != null)
                        {
                            Texture2D preview = AssetPreview.GetAssetPreview(tile.Prefab);
                            if (preview != null)
                            {
                                GUI.DrawTexture(tileRect, preview, ScaleMode.ScaleToFit);
                            }
                            else
                            {
                                EditorGUI.DrawRect(tileRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                                GUI.Label(tileRect, "No Preview", EditorStyles.centeredGreyMiniLabel);
                            }
                        }

                        // 선택 영역 표시
                        if (i == selectedTileIndex)
                        {
                            Color oldColor = GUI.color;
                            GUI.color = new Color(0, 0.8f, 1, 0.8f);
                            GUI.Box(tileRect, "", EditorStyles.selectionRect);
                            GUI.color = oldColor;
                        }

                        // 클릭 처리
                        if (Event.current.type == EventType.MouseDown && tileRect.Contains(Event.current.mousePosition))
                        {
                            selectedTileIndex = i;
                            currentMode = PlacementMode.Tile;
                            GUI.changed = true;
                            Event.current.Use();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("타일셋이 비어있습니다. 타일을 추가하세요.");
                }

                EditorGUILayout.EndScrollView();

                // 타일 추가/제거 버튼
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("타일 추가"))
                {
                    AddTile(selectedTileset);
                }

                if (GUILayout.Button("타일 제거") && selectedTileset.Tiles.Count > 0)
                {
                    RemoveTile(selectedTileset, selectedTileIndex);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("타일셋이 없습니다. '타일셋 추가' 버튼을 클릭하여 새 타일셋을 만드세요.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLayerPanel()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("레이어", boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("레이어 추가", GUILayout.Width(100)))
        {
            AddLayer();
        }

        if (GUILayout.Button("레이어 제거", GUILayout.Width(100)) && layers.Count > 1)
        {
            RemoveLayer(activeLayerIndex);
        }
        EditorGUILayout.EndHorizontal();

        // 레이어 목록
        layerScrollPosition = EditorGUILayout.BeginScrollView(layerScrollPosition, GUILayout.Height(150));

        for (int i = layers.Count - 1; i >= 0; i--)
        {
            LayerData layer = layers[i];

            EditorGUILayout.BeginHorizontal();

            // 레이어 가시성 토글
            bool newVisible = EditorGUILayout.Toggle(layer.Visible, GUILayout.Width(20));
            if (newVisible != layer.Visible)
            {
                layer.Visible = newVisible;
                SetLayerVisibility(i, newVisible);
            }

            // 레이어 잠금 토글
            bool newLocked = EditorGUILayout.Toggle(layer.Locked, GUILayout.Width(20));
            if (newLocked != layer.Locked)
            {
                layer.Locked = newLocked;
            }

            // 레이어 선택 버튼
            GUIStyle layerStyle = (i == activeLayerIndex) ? activeLayerStyle : inactiveLayerStyle;
            if (GUILayout.Button(layer.Name, layerStyle))
            {
                activeLayerIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            // 선택된 레이어 이름 편집
            if (i == activeLayerIndex)
            {
                layer.Name = EditorGUILayout.TextField("이름 변경", layer.Name);
            }
        }

        EditorGUILayout.EndScrollView();

        // 레이어 순서 변경 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↑") && activeLayerIndex > 0)
        {
            SwapLayers(activeLayerIndex, activeLayerIndex - 1);
        }

        if (GUILayout.Button("↓") && activeLayerIndex < layers.Count - 1)
        {
            SwapLayers(activeLayerIndex, activeLayerIndex + 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawToolPanel()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("도구", boldLabel);

        // 배치 모드 선택
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = (currentMode == PlacementMode.Tile) ? Color.cyan : Color.white;
        if (GUILayout.Button("타일", EditorStyles.miniButtonLeft))
        {
            currentMode = PlacementMode.Tile;
        }

        GUI.backgroundColor = (currentMode == PlacementMode.Platform) ? Color.cyan : Color.white;
        if (GUILayout.Button("플랫폼", EditorStyles.miniButtonMid))
        {
            currentMode = PlacementMode.Platform;
        }

        GUI.backgroundColor = (currentMode == PlacementMode.Enemy) ? Color.cyan : Color.white;
        if (GUILayout.Button("적", EditorStyles.miniButtonMid))
        {
            currentMode = PlacementMode.Enemy;
        }

        GUI.backgroundColor = (currentMode == PlacementMode.Collectible) ? Color.cyan : Color.white;
        if (GUILayout.Button("수집품", EditorStyles.miniButtonMid))
        {
            currentMode = PlacementMode.Collectible;
        }

        GUI.backgroundColor = (currentMode == PlacementMode.Eraser) ? Color.cyan : Color.white;
        if (GUILayout.Button("지우개", EditorStyles.miniButtonMid))
        {
            currentMode = PlacementMode.Eraser;
        }

        GUI.backgroundColor = (currentMode == PlacementMode.Select) ? Color.cyan : Color.white;
        if (GUILayout.Button("선택", EditorStyles.miniButtonRight))
        {
            currentMode = PlacementMode.Select;
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // 브러시 설정 (타일 모드에서만)
        if (currentMode == PlacementMode.Tile)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("브러시 설정", EditorStyles.boldLabel);

            // 브러시 유형
            currentBrushType = (BrushType)EditorGUILayout.EnumPopup("브러시 유형", currentBrushType);

            // 브러시 크기 (Rectangle 모드에서만)
            if (currentBrushType == BrushType.Rectangle)
            {
                brushSize.x = EditorGUILayout.IntSlider("너비", brushSize.x, 1, 10);
                brushSize.y = EditorGUILayout.IntSlider("높이", brushSize.y, 1, 10);
            }

            // 랜덤 타일 사용 옵션
            randomizeTiles = EditorGUILayout.Toggle("랜덤 타일", randomizeTiles);

            // 타일 변형 (회전/미러링)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("타일 변형", EditorStyles.boldLabel);
            currentTileVariant = (TileVariant)EditorGUILayout.EnumPopup("변형", currentTileVariant);

            // 변형 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("좌우반전"))
            {
                currentTileVariant = (currentTileVariant == TileVariant.FlipX) ? TileVariant.Normal : TileVariant.FlipX;
            }

            if (GUILayout.Button("상하반전"))
            {
                currentTileVariant = (currentTileVariant == TileVariant.FlipY) ? TileVariant.Normal : TileVariant.FlipY;
            }

            if (GUILayout.Button("90° 회전"))
            {
                RotateTileVariant(90);
            }
            EditorGUILayout.EndHorizontal();
        }

        // 선택 도구 옵션 (선택 모드에서만)
        if (currentMode == PlacementMode.Select && selectedObjects.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("선택 옵션", EditorStyles.boldLabel);

            if (GUILayout.Button("선택 복제"))
            {
                DuplicateSelectedObjects();
            }

            if (GUILayout.Button("선택 삭제"))
            {
                DeleteSelectedObjects();
            }

            if (GUILayout.Button("선택 해제"))
            {
                ClearSelection();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("맵 설정", boldLabel);

        // 맵 이름 및 저장/불러오기
        mapName = EditorGUILayout.TextField("맵 이름:", mapName);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("새 맵"))
        {
            if (EditorUtility.DisplayDialog("새 맵 만들기", "모든 배치된 오브젝트가 삭제됩니다. 계속하시겠습니까?", "예", "아니오"))
            {
                ClearMap();
            }
        }

        if (GUILayout.Button("맵 저장"))
        {
            SaveMap();
        }

        if (GUILayout.Button("맵 불러오기"))
        {
            LoadMap();
        }
        EditorGUILayout.EndHorizontal();

        // 그리드 설정
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("그리드 설정", EditorStyles.boldLabel);

        snapToGrid = EditorGUILayout.Toggle("그리드 스냅", snapToGrid);
        snapValue = EditorGUILayout.FloatField("스냅 값", snapValue);

        // 미리보기 설정
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미리보기 설정", EditorStyles.boldLabel);

        showPreview = EditorGUILayout.Toggle("미리보기 보기", showPreview);
        previewTint = EditorGUILayout.ColorField("미리보기 색상", previewTint);

        // 도움말
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "단축키:\n" +
            "Ctrl+Z: 실행 취소\n" +
            "Ctrl+Y: 다시 실행\n" +
            "Delete: 선택 삭제\n" +
            "Ctrl+D: 선택 복제\n" +
            "R: 타일 회전\n" +
            "F: 타일 좌우반전\n" +
            "Shift+F: 타일 상하반전\n" +
            "Shift+마우스 드래그: 선택 영역 지정\n" +
            "Alt+마우스 드래그: 선택 영역 이동",
            MessageType.Info);

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Scene GUI
    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // GUI 렌더링
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 300, 20), $"현재 레이어: {layers[activeLayerIndex].Name}", EditorStyles.boldLabel);
        Handles.EndGUI();

        // 선택 영역 표시
        if (isSelecting)
        {
            Handles.color = new Color(0, 0.8f, 1, 0.3f);
            Vector3 p1 = new Vector3(selectionStart.x, selectionStart.y, 0);
            Vector3 p2 = new Vector3(selectionEnd.x, selectionStart.y, 0);
            Vector3 p3 = new Vector3(selectionEnd.x, selectionEnd.y, 0);
            Vector3 p4 = new Vector3(selectionStart.x, selectionEnd.y, 0);

            Handles.DrawSolidRectangleWithOutline(new Vector3[] { p1, p2, p3, p4 },
                new Color(0, 0.5f, 1, 0.2f), new Color(0, 0.8f, 1, 0.8f));
        }

        // 미리보기 표시 (미리보기가 활성화되어 있을 때)
        if (showPreview && currentMode != PlacementMode.Eraser && currentMode != PlacementMode.Select)
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            if (snapToGrid)
            {
                mousePosition = SnapToGrid(mousePosition);
            }

            UpdatePreview(mousePosition);
        }
        else
        {
            DestroyPreview();
        }

        // 선택된 오브젝트 표시
        foreach (MapObject obj in selectedObjects)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(instance.transform.position, new Vector3(1.2f, 1.2f, 0.1f));
            }
        }

        // 입력 처리
        HandleInputs(e, sceneView);

        // 씬 뷰 갱신
        if (GUI.changed)
        {
            sceneView.Repaint();
        }
    }

    private void HandleInputs(Event e, SceneView sceneView)
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        if (snapToGrid)
        {
            mousePosition = SnapToGrid(mousePosition);
        }

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // 왼쪽 마우스 클릭
                {
                    HandleLeftMouseDown(e, mousePosition);
                }
                else if (e.button == 1) // 오른쪽 마우스 클릭
                {
                    HandleRightMouseDown(e, mousePosition);
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0) // 왼쪽 마우스 해제
                {
                    HandleLeftMouseUp(e, mousePosition);
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0) // 왼쪽 마우스 드래그
                {
                    HandleLeftMouseDrag(e, mousePosition);
                }
                break;

            case EventType.KeyDown:
                HandleKeyDown(e);
                break;
        }
    }

    private void HandleLeftMouseDown(Event e, Vector2 mousePosition)
    {
        // 현재 레이어가 잠겨 있으면 무시
        if (layers[activeLayerIndex].Locked)
        {
            return;
        }

        switch (currentMode)
        {
            case PlacementMode.Tile:
            case PlacementMode.Platform:
            case PlacementMode.Enemy:
            case PlacementMode.Collectible:
                // Shift+클릭 또는 일반 클릭으로 오브젝트 배치
                PlaceObject(mousePosition);
                break;

            case PlacementMode.Eraser:
                // 삭제 도구
                RemoveObject(mousePosition);
                break;

            case PlacementMode.Select:
                // 선택 도구
                if (e.shift)
                {
                    // Shift 키를 누른 상태로 드래그 시작 - 선택 영역 시작
                    isSelecting = true;
                    selectionStart = mousePosition;
                    selectionEnd = mousePosition;
                }
                else if (e.alt)
                {
                    // Alt 키를 누른 상태에서는 선택된 오브젝트 이동 시작
                    BeginMoveSelectedObjects();
                }
                else
                {
                    // 일반 클릭 - 오브젝트 선택/선택 해제
                    SelectObjectAt(mousePosition, e.control);
                }
                break;
        }
    }

    private void HandleRightMouseDown(Event e, Vector2 mousePosition)
    {
        // 오른쪽 클릭으로 컨텍스트 메뉴 표시
        GenericMenu menu = new GenericMenu();

        if (currentMode == PlacementMode.Select && selectedObjects.Count > 0)
        {
            menu.AddItem(new GUIContent("복제"), false, () => DuplicateSelectedObjects());
            menu.AddItem(new GUIContent("삭제"), false, () => DeleteSelectedObjects());
            menu.AddItem(new GUIContent("선택 해제"), false, () => ClearSelection());
            menu.AddSeparator("");
        }

        menu.AddItem(new GUIContent("타일 모드"), currentMode == PlacementMode.Tile, () => currentMode = PlacementMode.Tile);
        menu.AddItem(new GUIContent("플랫폼 모드"), currentMode == PlacementMode.Platform, () => currentMode = PlacementMode.Platform);
        menu.AddItem(new GUIContent("적 모드"), currentMode == PlacementMode.Enemy, () => currentMode = PlacementMode.Enemy);
        menu.AddItem(new GUIContent("수집품 모드"), currentMode == PlacementMode.Collectible, () => currentMode = PlacementMode.Collectible);
        menu.AddItem(new GUIContent("지우개 모드"), currentMode == PlacementMode.Eraser, () => currentMode = PlacementMode.Eraser);
        menu.AddItem(new GUIContent("선택 모드"), currentMode == PlacementMode.Select, () => currentMode = PlacementMode.Select);

        menu.ShowAsContext();
        e.Use();
    }

    private void HandleLeftMouseUp(Event e, Vector2 mousePosition)
    {
        if (currentMode == PlacementMode.Select && isSelecting)
        {
            // 선택 영역 완료
            isSelecting = false;
            SelectObjectsInArea(selectionStart, selectionEnd, e.control);
            e.Use();
        }
    }

    private void HandleLeftMouseDrag(Event e, Vector2 mousePosition)
    {
        // 현재 레이어가 잠겨 있으면 무시
        if (layers[activeLayerIndex].Locked)
        {
            return;
        }

        switch (currentMode)
        {
            case PlacementMode.Tile:
            case PlacementMode.Platform:
            case PlacementMode.Enemy:
            case PlacementMode.Collectible:
                // 연속 배치
                PlaceObject(mousePosition);
                break;

            case PlacementMode.Eraser:
                // 연속 삭제
                RemoveObject(mousePosition);
                break;

            case PlacementMode.Select:
                if (isSelecting)
                {
                    // 선택 영역 업데이트
                    selectionEnd = mousePosition;
                }
                else if (e.alt && selectedObjects.Count > 0)
                {
                    // 선택된 오브젝트 이동
                    MoveSelectedObjects(mousePosition);
                }
                break;
        }

        e.Use();
    }

    private void HandleKeyDown(Event e)
    {
        // 키보드 단축키 처리 (씬 뷰에서)
        if (e.keyCode == KeyCode.Delete && selectedObjects.Count > 0)
        {
            DeleteSelectedObjects();
            e.Use();
        }
        else if (e.control && e.keyCode == KeyCode.D && selectedObjects.Count > 0)
        {
            DuplicateSelectedObjects();
            e.Use();
        }
        else if (e.keyCode == KeyCode.R)
        {
            RotateTileVariant(90);
            e.Use();
        }
        else if (e.keyCode == KeyCode.F)
        {
            if (e.shift)
            {
                // 상하반전
                currentTileVariant = (currentTileVariant == TileVariant.FlipY) ? TileVariant.Normal : TileVariant.FlipY;
            }
            else
            {
                // 좌우반전
                currentTileVariant = (currentTileVariant == TileVariant.FlipX) ? TileVariant.Normal : TileVariant.FlipX;
            }
            e.Use();
        }
    }
    #endregion

    #region Process Keyboard Shortcuts
    private void ProcessKeyboardShortcuts()
    {
        // 키보드 단축키 처리 (GUI에서)
        Event e = Event.current;
        
        if (e.type == EventType.KeyDown)
        {
            if (e.control && e.keyCode == KeyCode.Z)
            {
                // Ctrl+Z: 실행 취소
                Undo();
                e.Use();
            }
            else if (e.control && e.keyCode == KeyCode.Y)
            {
                // Ctrl+Y: 다시 실행
                Redo();
                e.Use();
            }
        }
    }
    #endregion

    #region Map Actions
    private void PlaceObject(Vector2 position)
    {
        // 현재 모드와 타일셋에 따라 오브젝트 배치
        if (tilesets.Count == 0 || selectedTilesetIndex >= tilesets.Count)
        {
            return;
        }

        GameObject prefab = null;
        
        switch (currentMode)
        {
            case PlacementMode.Tile:
                TilesetData selectedTileset = tilesets[selectedTilesetIndex];
                if (selectedTileset.Tiles.Count == 0 || selectedTileIndex >= selectedTileset.Tiles.Count)
                {
                    return;
                }

                TileData selectedTile = selectedTileset.Tiles[selectedTileIndex];
                prefab = selectedTile.Prefab;
                break;

            case PlacementMode.Platform:
                // 플랫폼 프리팹 선택 로직
                prefab = GetDefaultPrefab("Platform");
                break;

            case PlacementMode.Enemy:
                // 적 프리팹 선택 로직
                prefab = GetDefaultPrefab("Enemy");
                break;

            case PlacementMode.Collectible:
                // 수집품 프리팹 선택 로직
                prefab = GetDefaultPrefab("Collectible");
                break;
        }

        if (prefab == null)
        {
            return;
        }

        // 이미 동일한 위치에 오브젝트가 있는지 확인
        if (IsPositionOccupied(position))
        {
            return;
        }

        // 새 오브젝트 생성
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.transform.position = new Vector3(position.x, position.y, 0);

        // 타일 변형 적용 (회전, 반전 등)
        ApplyTileVariant(instance);

        // 맵 오브젝트 데이터 생성
        MapObject mapObject = new MapObject
        {
            PrefabID = prefab.GetInstanceID(),
            InstanceID = instance.GetInstanceID(),
            Position = position,
            Layer = activeLayerIndex,
            Variant = currentTileVariant
        };

        // 맵 오브젝트 목록에 추가
        mapObjects.Add(mapObject);

        // 실행 취소 작업 등록
        RegisterUndoAction(new UndoRedoAction
        {
            Type = UndoRedoActionType.Create,
            Objects = new List<MapObject> { mapObject }
        });

        EditorUtility.SetDirty(instance);
    }

    private GameObject GetDefaultPrefab(string type)
    {
        // 기본 프리팹 찾기 로직 (구현 필요)
        // 예: 리소스 폴더에서 프리팹 로드
        return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/02_Prefabs/Map/{type}/{type}Default.prefab");
    }

    private void RemoveObject(Vector2 position)
    {
        // 위치에 있는 오브젝트 찾기
        List<MapObject> objectsToRemove = FindObjectsAt(position);
        if (objectsToRemove.Count == 0)
        {
            return;
        }

        // 삭제할 오브젝트 저장 (실행 취소용)
        RegisterUndoAction(new UndoRedoAction
        {
            Type = UndoRedoActionType.Delete,
            Objects = new List<MapObject>(objectsToRemove)
        });

        // 오브젝트 삭제
        foreach (MapObject obj in objectsToRemove)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
            mapObjects.Remove(obj);
        }
    }

    private List<MapObject> FindObjectsAt(Vector2 position)
    {
        List<MapObject> result = new List<MapObject>();
        float pickingDistance = 0.5f; // 피킹 범위

        foreach (MapObject obj in mapObjects)
        {
            // 현재 레이어가 아니거나 레이어가 숨겨져 있으면 건너뛰기
            if (obj.Layer != activeLayerIndex || !layers[obj.Layer].Visible)
            {
                continue;
            }

            float distance = Vector2.Distance(obj.Position, position);
            if (distance <= pickingDistance)
            {
                result.Add(obj);
            }
        }

        return result;
    }

    private void SelectObjectAt(Vector2 position, bool additive)
    {
        List<MapObject> objectsAtPosition = FindObjectsAt(position);
        
        if (!additive)
        {
            // 기존 선택 해제 (Ctrl 키를 누르지 않은 경우)
            ClearSelection();
        }

        if (objectsAtPosition.Count > 0)
        {
            // 이미 선택된 오브젝트인 경우 선택 해제
            if (selectedObjects.Contains(objectsAtPosition[0]))
            {
                selectedObjects.Remove(objectsAtPosition[0]);
            }
            else
            {
                // 오브젝트 선택
                selectedObjects.Add(objectsAtPosition[0]);
            }
        }
    }

    private void SelectObjectsInArea(Vector2 start, Vector2 end, bool additive)
    {
        // 선택 영역 정규화
        Vector2 min = new Vector2(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y));
        Vector2 max = new Vector2(Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));

        if (!additive)
        {
            // 기존 선택 해제 (Ctrl 키를 누르지 않은 경우)
            ClearSelection();
        }

        // 영역 내에 있는 모든 오브젝트 찾기
        foreach (MapObject obj in mapObjects)
        {
            // 현재 레이어가 아니거나 레이어가 숨겨져 있으면 건너뛰기
            if (!layers[obj.Layer].Visible)
            {
                continue;
            }

            if (obj.Position.x >= min.x && obj.Position.x <= max.x &&
                obj.Position.y >= min.y && obj.Position.y <= max.y)
            {
                // 이미 선택된 오브젝트가 아니면 추가
                if (!selectedObjects.Contains(obj))
                {
                    selectedObjects.Add(obj);
                }
            }
        }
    }

    private void ClearSelection()
    {
        selectedObjects.Clear();
    }

    private void BeginMoveSelectedObjects()
    {
        // 이동 시작 - 필요한 초기화 작업
    }

    private void MoveSelectedObjects(Vector2 newPosition)
    {
        // 선택된 오브젝트 이동 구현
        // 현재는 단순 이동, 실제로는 이전 위치에서의 오프셋을 계산해야 함
        
        foreach (MapObject obj in selectedObjects)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                Vector3 updatedPosition = new Vector3(newPosition.x, newPosition.y, instance.transform.position.z);
                if (snapToGrid)
                {
                    updatedPosition = SnapToGrid(updatedPosition);
                }
                
                instance.transform.position = updatedPosition;
                obj.Position = new Vector2(updatedPosition.x, updatedPosition.y);
                EditorUtility.SetDirty(instance);
            }
        }
    }

    private void DuplicateSelectedObjects()
    {
        if (selectedObjects.Count == 0)
        {
            return;
        }

        List<MapObject> originalSelection = new List<MapObject>(selectedObjects);
        ClearSelection();
        
        // 복제된 오브젝트를 저장할 목록
        List<MapObject> duplicatedObjects = new List<MapObject>();

        foreach (MapObject original in originalSelection)
        {
            // 원본 프리팹 찾기
            UnityEngine.Object prefab = EditorUtility.InstanceIDToObject(original.PrefabID);
            if (prefab == null)
            {
                continue;
            }

            // 새 인스턴스 생성
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                // 원본 위치에서 약간 이동시킨 위치에 배치
                Vector2 newPosition = original.Position + new Vector2(1, 0);
                if (snapToGrid)
                {
                    newPosition = SnapToGrid(newPosition);
                }
                
                instance.transform.position = new Vector3(newPosition.x, newPosition.y, 0);
                
                // 변형 적용
                instance.transform.localScale = GetScaleForVariant(original.Variant);
                instance.transform.rotation = GetRotationForVariant(original.Variant);
                
                // 새 맵 오브젝트 생성
                MapObject newMapObject = new MapObject
                {
                    PrefabID = original.PrefabID,
                    InstanceID = instance.GetInstanceID(),
                    Position = newPosition,
                    Layer = original.Layer,
                    Variant = original.Variant
                };
                
                mapObjects.Add(newMapObject);
                duplicatedObjects.Add(newMapObject);
                selectedObjects.Add(newMapObject);
                
                EditorUtility.SetDirty(instance);
            }
        }
        
        // 실행 취소 작업 등록
        if (duplicatedObjects.Count > 0)
        {
            RegisterUndoAction(new UndoRedoAction
            {
                Type = UndoRedoActionType.Create,
                Objects = duplicatedObjects
            });
        }
    }

    private void DeleteSelectedObjects()
    {
        if (selectedObjects.Count == 0)
        {
            return;
        }

        // 삭제할 오브젝트 저장 (실행 취소용)
        RegisterUndoAction(new UndoRedoAction
        {
            Type = UndoRedoActionType.Delete,
            Objects = new List<MapObject>(selectedObjects)
        });

        // 오브젝트 삭제
        foreach (MapObject obj in selectedObjects)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
            mapObjects.Remove(obj);
        }

        // 선택 초기화
        selectedObjects.Clear();
    }
    #endregion

    #region Utility Functions
    private Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(
            Mathf.Round(position.x / snapValue) * snapValue,
            Mathf.Round(position.y / snapValue) * snapValue
        );
    }

    private void ApplyTileVariant(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.transform.localScale = GetScaleForVariant(currentTileVariant);
        instance.transform.rotation = GetRotationForVariant(currentTileVariant);
    }

    private Vector3 GetScaleForVariant(TileVariant variant)
    {
        Vector3 scale = Vector3.one;

        switch (variant)
        {
            case TileVariant.FlipX:
                scale.x = -1;
                break;
            case TileVariant.FlipY:
                scale.y = -1;
                break;
            case TileVariant.Rotate180:
                scale.x = -1;
                scale.y = -1;
                break;
        }

        return scale;
    }

    private Quaternion GetRotationForVariant(TileVariant variant)
    {
        float angle = 0;

        switch (variant)
        {
            case TileVariant.Rotate90:
                angle = 90;
                break;
            case TileVariant.Rotate180:
                angle = 180;
                break;
            case TileVariant.Rotate270:
                angle = 270;
                break;
        }

        return Quaternion.Euler(0, 0, angle);
    }

    private void RotateTileVariant(float angle)
    {
        switch (currentTileVariant)
        {
            case TileVariant.Normal:
                currentTileVariant = TileVariant.Rotate90;
                break;
            case TileVariant.Rotate90:
                currentTileVariant = TileVariant.Rotate180;
                break;
            case TileVariant.Rotate180:
                currentTileVariant = TileVariant.Rotate270;
                break;
            case TileVariant.Rotate270:
                currentTileVariant = TileVariant.Normal;
                break;
            default:
                currentTileVariant = TileVariant.Rotate90;
                break;
        }
    }

    private bool IsPositionOccupied(Vector2 position)
    {
        float threshold = 0.1f;

        foreach (MapObject obj in mapObjects)
        {
            // 같은 레이어의 오브젝트만 확인
            if (obj.Layer == activeLayerIndex)
            {
                float distance = Vector2.Distance(obj.Position, position);
                if (distance < threshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdatePreview(Vector2 position)
    {
        if (tilesets.Count == 0 || selectedTilesetIndex >= tilesets.Count)
        {
            DestroyPreview();
            return;
        }

        GameObject prefab = null;

        switch (currentMode)
        {
            case PlacementMode.Tile:
                TilesetData selectedTileset = tilesets[selectedTilesetIndex];
                if (selectedTileset.Tiles.Count == 0 || selectedTileIndex >= selectedTileset.Tiles.Count)
                {
                    DestroyPreview();
                    return;
                }
                prefab = selectedTileset.Tiles[selectedTileIndex].Prefab;
                break;

            case PlacementMode.Platform:
                prefab = GetDefaultPrefab("Platform");
                break;

            case PlacementMode.Enemy:
                prefab = GetDefaultPrefab("Enemy");
                break;

            case PlacementMode.Collectible:
                prefab = GetDefaultPrefab("Collectible");
                break;
        }

        if (prefab == null)
        {
            DestroyPreview();
            return;
        }

        // 미리보기 오브젝트 생성/업데이트
        if (previewInstance == null)
        {
            previewInstance = Instantiate(prefab);
            previewInstance.name = "Preview";
            
            // 플레이모드를 방지하기 위한 DontSave 설정
            previewInstance.hideFlags = HideFlags.DontSave;
            
            // 렌더러 찾기
            Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // 머티리얼의 복사본 생성
                Material previewMaterial = new Material(renderer.sharedMaterial);
                previewMaterial.color = new Color(
                    previewMaterial.color.r * previewTint.r,
                    previewMaterial.color.g * previewTint.g,
                    previewMaterial.color.b * previewTint.b,
                    previewTint.a
                );
                renderer.material = previewMaterial;
            }
        }

        // 미리보기 위치 업데이트
        previewInstance.transform.position = new Vector3(position.x, position.y, 0);
        
        // 변형 적용
        previewInstance.transform.localScale = GetScaleForVariant(currentTileVariant);
        previewInstance.transform.rotation = GetRotationForVariant(currentTileVariant);
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void SetLayerVisibility(int layerIndex, bool visible)
    {
        foreach (MapObject obj in mapObjects)
        {
            if (obj.Layer == layerIndex)
            {
                GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                if (instance != null)
                {
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = visible;
                    }
                }
            }
        }
    }
    #endregion

    #region Tileset and Layer Management
    private void AddTileset()
    {
        string path = EditorUtility.OpenFilePanelWithFilters("타일셋 프리팹 폴더 선택", "Assets", new string[] { "Prefabs", "prefab" });
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // 상대 경로로 변환
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        // 폴더 내의 모든 프리팹 가져오기
        string folderName = Path.GetFileName(path);
        TilesetData newTileset = new TilesetData
        {
            Name = folderName,
            Tiles = new List<TileData>()
        };

        string[] prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { path });
        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                newTileset.Tiles.Add(new TileData { Prefab = prefab });
            }
        }

        tilesets.Add(newTileset);
        selectedTilesetIndex = tilesets.Count - 1;
    }

    private void RemoveTileset(int index)
    {
        if (index >= 0 && index < tilesets.Count)
        {
            tilesets.RemoveAt(index);
            selectedTilesetIndex = Mathf.Clamp(selectedTilesetIndex, 0, tilesets.Count - 1);
        }
    }

    private void AddTile(TilesetData tileset)
    {
        string path = EditorUtility.OpenFilePanel("타일 프리팹 선택", "Assets", "prefab");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // 상대 경로로 변환
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            tileset.Tiles.Add(new TileData { Prefab = prefab });
        }
    }

    private void RemoveTile(TilesetData tileset, int index)
    {
        if (index >= 0 && index < tileset.Tiles.Count)
        {
            tileset.Tiles.RemoveAt(index);
            selectedTileIndex = Mathf.Clamp(selectedTileIndex, 0, tileset.Tiles.Count - 1);
        }
    }

    private void AddLayer()
    {
        string layerName = $"새 레이어 {layers.Count}";
        layers.Add(new LayerData { Name = layerName, Visible = true, Locked = false });
        activeLayerIndex = layers.Count - 1;
    }

    private void RemoveLayer(int index)
    {
        if (layers.Count <= 1)
        {
            EditorUtility.DisplayDialog("레이어 제거 불가", "최소 하나의 레이어는 유지해야 합니다.", "확인");
            return;
        }

        if (index >= 0 && index < layers.Count)
        {
            if (EditorUtility.DisplayDialog("레이어 제거", $"'{layers[index].Name}' 레이어와 해당 레이어의 모든 오브젝트를 제거하시겠습니까?", "예", "아니오"))
            {
                // 해당 레이어의 오브젝트 삭제
                List<MapObject> objectsToRemove = mapObjects.Where(obj => obj.Layer == index).ToList();
                foreach (MapObject obj in objectsToRemove)
                {
                    GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                    if (instance != null)
                    {
                        DestroyImmediate(instance);
                    }
                    mapObjects.Remove(obj);
                }

                // 더 높은 레이어 인덱스를 가진 오브젝트들의 레이어 인덱스 조정
                foreach (MapObject obj in mapObjects)
                {
                    if (obj.Layer > index)
                    {
                        obj.Layer--;
                    }
                }

                // 레이어 제거
                layers.RemoveAt(index);
                
                // 활성 레이어 인덱스 조정
                activeLayerIndex = Mathf.Clamp(activeLayerIndex, 0, layers.Count - 1);
            }
        }
    }

    private void SwapLayers(int index1, int index2)
    {
        if (index1 >= 0 && index1 < layers.Count && index2 >= 0 && index2 < layers.Count)
        {
            // 레이어 순서 변경
            LayerData temp = layers[index1];
            layers[index1] = layers[index2];
            layers[index2] = temp;

            // 해당 레이어의 오브젝트 레이어 인덱스 업데이트
            foreach (MapObject obj in mapObjects)
            {
                if (obj.Layer == index1)
                {
                    obj.Layer = index2;
                }
                else if (obj.Layer == index2)
                {
                    obj.Layer = index1;
                }
            }

            // 활성 레이어 인덱스 업데이트
            if (activeLayerIndex == index1)
            {
                activeLayerIndex = index2;
            }
            else if (activeLayerIndex == index2)
            {
                activeLayerIndex = index1;
            }
        }
    }
    #endregion

    #region Save and Load
    private void SaveMap()
    {
        string path = EditorUtility.SaveFilePanel("맵 저장", "Assets", mapName, "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // 맵 데이터 생성
        MapSaveData saveData = new MapSaveData
        {
            Name = mapName,
            Objects = mapObjects,
            Layers = layers
        };

        // JSON으로 직렬화
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(path, json);

        Debug.Log($"맵 저장 완료: {path}");
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("맵 불러오기", "Assets", "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string json = File.ReadAllText(path);
        MapSaveData saveData = JsonUtility.FromJson<MapSaveData>(json);

        if (saveData == null)
        {
            EditorUtility.DisplayDialog("맵 불러오기 실패", "잘못된 맵 파일 형식입니다.", "확인");
            return;
        }

        // 기존 맵 클리어
        ClearMap();

        // 맵 이름 설정
        mapName = saveData.Name;

        // 레이어 불러오기
        layers = saveData.Layers;
        if (layers.Count == 0)
        {
            // 기본 레이어 추가
            layers.Add(new LayerData { Name = "기본 레이어", Visible = true, Locked = false });
        }
        activeLayerIndex = Mathf.Clamp(activeLayerIndex, 0, layers.Count - 1);

        // 맵 오브젝트 불러오기
        foreach (MapObject obj in saveData.Objects)
        {
            // 프리팹 ID로 프리팹 찾기
            UnityEngine.Object prefab = EditorUtility.InstanceIDToObject(obj.PrefabID);
            if (prefab == null)
            {
                // 맵을 저장할 때 사용했던 프리팹이 없는 경우, 건너뛰기
                continue;
            }

            // 인스턴스 생성
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position = new Vector3(obj.Position.x, obj.Position.y, 0);
                
                // 변형 적용
                instance.transform.localScale = GetScaleForVariant(obj.Variant);
                instance.transform.rotation = GetRotationForVariant(obj.Variant);
                
                // 오브젝트 ID 업데이트
                obj.InstanceID = instance.GetInstanceID();
                
                // 렌더러 가시성 설정
                if (obj.Layer < layers.Count && !layers[obj.Layer].Visible)
                {
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        // 맵 오브젝트 목록 업데이트
        mapObjects = saveData.Objects;

        Debug.Log($"맵 불러오기 완료: {path}");
    }

    private void ClearMap()
    {
        // 선택 해제
        ClearSelection();

        // 모든 맵 오브젝트 삭제
        foreach (MapObject obj in mapObjects)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
        }

        // 맵 오브젝트 목록 초기화
        mapObjects.Clear();

        // 언두/리두 히스토리 초기화
        undoActions.Clear();
        redoActions.Clear();
    }
    #endregion

    #region Undo/Redo System
    private void RegisterUndoAction(UndoRedoAction action)
    {
        // 새 작업 추가 시 리두 기록 초기화
        redoActions.Clear();
        
        // 언두 작업 추가
        undoActions.Add(action);
        
        // 최대 언두 단계 유지
        if (undoActions.Count > maxUndoSteps)
        {
            undoActions.RemoveAt(0);
        }
    }

    private void Undo()
    {
        if (undoActions.Count == 0)
        {
            return;
        }

        // 마지막 작업 가져오기
        UndoRedoAction action = undoActions[undoActions.Count - 1];
        undoActions.RemoveAt(undoActions.Count - 1);
        
        // 리두 기록에 추가
        redoActions.Add(action);
        
        // 작업 유형에 따른 처리
        switch (action.Type)
        {
            case UndoRedoActionType.Create:
                // 생성된 오브젝트 삭제
                foreach (MapObject obj in action.Objects)
                {
                    GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                    if (instance != null)
                    {
                        DestroyImmediate(instance);
                    }
                    mapObjects.Remove(obj);
                }
                break;
                
            case UndoRedoActionType.Delete:
                // 삭제된 오브젝트 복원
                foreach (MapObject obj in action.Objects)
                {
                    // 프리팹 ID로 프리팹 찾기
                    UnityEngine.Object prefab = EditorUtility.InstanceIDToObject(obj.PrefabID);
                    if (prefab != null)
                    {
                        // 인스턴스 생성
                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (instance != null)
                        {
                            instance.transform.position = new Vector3(obj.Position.x, obj.Position.y, 0);
                            
                            // 변형 적용
                            instance.transform.localScale = GetScaleForVariant(obj.Variant);
                            instance.transform.rotation = GetRotationForVariant(obj.Variant);
                            
                            // 새 인스턴스 ID 설정
                            obj.InstanceID = instance.GetInstanceID();
                            
                            // 맵 오브젝트 목록에 추가
                            mapObjects.Add(obj);
                        }
                    }
                }
                break;
                
            case UndoRedoActionType.Move:
                // 이동된 오브젝트 원래 위치로 복원
                foreach (MapObject obj in action.Objects)
                {
                    GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                    if (instance != null)
                    {
                        // 이전 위치로 되돌리기
                        Vector3 originalPosition = new Vector3(obj.OriginalPosition.x, obj.OriginalPosition.y, instance.transform.position.z);
                        instance.transform.position = originalPosition;
                        
                        // 현재 위치와 이전 위치 교환
                        Vector2 temp = obj.Position;
                        obj.Position = obj.OriginalPosition;
                        obj.OriginalPosition = temp;
                    }
                }
                break;
        }
    }

    private void Redo()
    {
        if (redoActions.Count == 0)
        {
            return;
        }

        // 마지막 리두 작업 가져오기
        UndoRedoAction action = redoActions[redoActions.Count - 1];
        redoActions.RemoveAt(redoActions.Count - 1);
        
        // 언두 기록에 추가
        undoActions.Add(action);
        
        // 작업 유형에 따른 처리
        switch (action.Type)
        {
            case UndoRedoActionType.Create:
                // 삭제된 오브젝트 다시 생성
                foreach (MapObject obj in action.Objects)
                {
                    // 프리팹 ID로 프리팹 찾기
                    UnityEngine.Object prefab = EditorUtility.InstanceIDToObject(obj.PrefabID);
                    if (prefab != null)
                    {
                        // 인스턴스 생성
                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (instance != null)
                        {
                            instance.transform.position = new Vector3(obj.Position.x, obj.Position.y, 0);
                            
                            // 변형 적용
                            instance.transform.localScale = GetScaleForVariant(obj.Variant);
                            instance.transform.rotation = GetRotationForVariant(obj.Variant);
                            
                            // 새 인스턴스 ID 설정
                            obj.InstanceID = instance.GetInstanceID();
                            
                            // 맵 오브젝트 목록에 추가
                            mapObjects.Add(obj);
                        }
                    }
                }
                break;
                
            case UndoRedoActionType.Delete:
                // 복원된 오브젝트 다시 삭제
                foreach (MapObject obj in action.Objects)
                {
                    GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                    if (instance != null)
                    {
                        DestroyImmediate(instance);
                    }
                    mapObjects.Remove(obj);
                }
                break;
                
            case UndoRedoActionType.Move:
                // 위치 복원된 오브젝트 다시 이동
                foreach (MapObject obj in action.Objects)
                {
                    GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
                    if (instance != null)
                    {
                        // 이전 이동 위치로 다시 이동
                        Vector3 movedPosition = new Vector3(obj.OriginalPosition.x, obj.OriginalPosition.y, instance.transform.position.z);
                        instance.transform.position = movedPosition;
                        
                        // 현재 위치와 이전 위치 교환
                        Vector2 temp = obj.Position;
                        obj.Position = obj.OriginalPosition;
                        obj.OriginalPosition = temp;
                    }
                }
                break;
        }
    }
    #endregion

    #region Data Classes
    [System.Serializable]
    public class TileData
    {
        public GameObject Prefab;
    }

    [System.Serializable]
    public class TilesetData
    {
        public string Name;
        public List<TileData> Tiles = new List<TileData>();
    }

    [System.Serializable]
    public class LayerData
    {
        public string Name;
        public bool Visible;
        public bool Locked;
    }

    [System.Serializable]
    public class MapObject
    {
        public int PrefabID;
        public int InstanceID;
        public Vector2 Position;
        public Vector2 OriginalPosition; // 이동 취소에 사용
        public int Layer;
        public TileVariant Variant;
    }

    [System.Serializable]
    public class MapSaveData
    {
        public string Name;
        public List<MapObject> Objects = new List<MapObject>();
        public List<LayerData> Layers = new List<LayerData>();
    }

    public enum UndoRedoActionType
    {
        Create,
        Delete,
        Move
    }

    [System.Serializable]
    public class UndoRedoAction
    {
        public UndoRedoActionType Type;
        public List<MapObject> Objects = new List<MapObject>();
    }
    #endregion
}