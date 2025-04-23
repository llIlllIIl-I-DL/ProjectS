using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditorInternal;
using System; // Exception 클래스 사용을 위해 추가

public class MetroidvaniaMapEditor : EditorWindow
{
    // 에디터 상태 열거형 추가
    private enum EditorState
    {
        Normal,
        DraggingConnection
    }
    
    private EditorState currentState = EditorState.Normal;
    
    private List<RoomModule> availableModules = new List<RoomModule>();
    private List<PlacedModule> placedModules = new List<PlacedModule>();
    private RoomModule selectedModule;
    private Vector2 scrollPosition;
    private Vector2 mapScrollPosition;
    private float zoomLevel = 1.0f;
    private Vector2 mapOffset = Vector2.zero;
    private GUIStyle moduleButtonStyle;
    private PlacedModule selectedPlacedModule;
    private bool isPlacingModule = false;
    private int draggingConnectionIndex = -1;
    private PlacedModule draggingModule;
    private PlacedModule draggingConnectionModule;
    
    // 드래그 라인 시작점과 끝점
    private Vector2 dragStartPos;
    private Vector2 dragEndPos;
    
    // 마지막 드로우 타임 변수 추가
    private float lastDrawTime = 0f;

    // 성능 최적화 변수
    private float lastRepaintTime = 0f;
    private Vector2 lastMousePos;
    private const float DEFAULT_REPAINT_INTERVAL = 0.1f; // 기본 10fps
    private float repaintInterval = 0.1f; // 실제 사용할 가변 인터벌
    private float lastModeChangeTime = 0f; // 모드 변경 시점 추적
    private Dictionary<int, Matrix4x4> rotationMatrices = new Dictionary<int, Matrix4x4>();
    private List<PlacedModule> modulePool = new List<PlacedModule>();
    private bool enableDynamicRendering = true;
    private bool showConnectionPoints = true;
    private int renderQuality = 1; // 0: 낮음, 1: 중간, 2: 높음
    private bool emergencyMode = false; // 응급 모드 추가
    private bool ultraSimpleMode = false; // 초극한 단순화 모드
    private const float GRID_SIZE = 1f; // 모듈 그리드 기본 크기
    private const float TILE_WIDTH = 1.0f; // 모듈 타일 가로 크기
    private const float TILE_HEIGHT = 1.0f; // 모듈 타일 세로 크기
    private bool useRoomTileGrid = true; // 룸 타일 그리드 사용 여부
    
    // 유니티 타일맵과 모듈 타일 크기 비율
    private const int UNITY_TILES_PER_MODULE_TILE = 10; // 모듈 1타일 = 유니티 타일맵 10칸

    private string mapName = "New Map";
    private string savePath = "Assets/Resources/Maps";
    private string modulePath = "Assets/03_Scripts/RoomModules/";
    
    // 모듈 생성에 필요한 변수들
    private string moduleName = "";
    private GameObject modulePrefab;
    private Texture2D thumbnail;
    private RoomModule.ModuleCategory category;
    private RoomModule.EnvironmentTheme theme; // 환경 테마 필드 추가
    private bool isSpecialRoom;
    private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();
    
    // 모듈 프리뷰 관련 변수
    private List<int> compatibleConnectionPoints = new List<int>();
    private PlacedModule nearestCompatibleModule;
    private int nearestCompatiblePointIndex = -1;

    // 배치된 모듈 정보를 저장하는 클래스
    private class PlacedModule
    {
        public RoomModule moduleData;
        public Vector2 position;
        public int rotationStep; // 90도 단위 회전
        public List<ConnectionInfo> connections = new List<ConnectionInfo>();

        public class ConnectionInfo
        {
            public int connectionPointIndex;
            public PlacedModule connectedModule;
            public int connectedPointIndex;
        }
    }

    [MenuItem("Metroidvania/Map Editor")]
    public static void ShowWindow()
    {
        // 에디터 윈도우를 열 때 문자열 변수를 사용하여 타이틀 설정
        string windowTitle = "Metroidvania Map Editor";
        EditorWindow window = GetWindow(typeof(MetroidvaniaMapEditor));
        window.titleContent = new GUIContent(windowTitle);
    }

    private void OnEnable()
    {
        // 모든 RoomModule 에셋 로드
        LoadModules();

        // GUI 스타일 초기화는 OnGUI에서 수행
        moduleButtonStyle = null;
    }

    private void LoadModules()
    {
        availableModules.Clear();

        // 프로젝트 내의 모든 RoomModule 에셋을 찾음
        string[] guids = AssetDatabase.FindAssets("t:RoomModule");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(path);
            if (module != null)
            {
                availableModules.Add(module);
            }
        }
    }

    private void OnGUI()
    {
        try
        {
            // 예외 처리로 GUILayout 상태 오류가 발생하더라도 창이 계속 작동하도록 함
            
            // GUI 스타일 초기화
            if (moduleButtonStyle == null)
            {
                InitializeGUIStyles();
            }

            // 화면 분할
            float topPanelHeight = position.height * 0.2f;
            float bottomPanelHeight = position.height * 0.3f;
            
            // 상단패널 (모듈 선택)
            Rect topPanel = new Rect(0, 0, position.width, topPanelHeight);
            DrawModuleSelectionPanel(topPanel);
            
            // 중앙패널 (맵 영역)
            Rect mapPanel = new Rect(0, topPanelHeight, position.width, 
                                  position.height - topPanelHeight - bottomPanelHeight);
            
            GUI.Box(mapPanel, "");
            
            // 맵 내부 여백
            float mapMargin = 5f;
            Rect mapRect = new Rect(
                mapPanel.x + mapMargin, 
                mapPanel.y + mapMargin, 
                mapPanel.width - (mapMargin * 2), 
                mapPanel.height - (mapMargin * 2)
            );
            
            // 모드 변경 버튼
            Rect modeSwitchRect = new Rect(mapPanel.x + 10, mapPanel.y + 10, 110, 20);
            if (GUI.Button(modeSwitchRect, "모드전환"))
            {
                if (emergencyMode)
                {
                    emergencyMode = false;
                    ultraSimpleMode = true;
                    // 리페인트 주기 변경
                    repaintInterval = 0.3f; // 초극한 단순화 모드 (약 3fps)
                }
                else if (ultraSimpleMode)
                {
                    ultraSimpleMode = false;
                    emergencyMode = false;
                    // 리페인트 주기 변경
                    repaintInterval = 0.1f; // 일반 모드 (약 10fps)
                }
                else
                {
                    emergencyMode = true;
                    ultraSimpleMode = false;
                    // 리페인트 주기 변경
                    repaintInterval = 0.2f; // 비상 모드 (약 5fps)
                }
            }
            
            // 배치 모드일 때 취소 버튼 표시
            if (selectedModule != null)
            {
                Rect cancelPlacementRect = new Rect(mapPanel.x + 130, mapPanel.y + 10, 120, 20);
                if (GUI.Button(cancelPlacementRect, "배치 취소 (ESC)"))
                {
                    CancelModulePlacement();
                }
                
                // 모드 상태 표시
                Rect placementModeRect = new Rect(mapPanel.x + 260, mapPanel.y + 10, 200, 20);
                GUI.Label(placementModeRect, "현재 모드: 모듈 배치 중", EditorStyles.boldLabel);
            }
            
            // 맵 그리기
            DrawMap(mapRect);
            
            // 이벤트 처리
            HandleMapEvents(Event.current, mapRect);
            
            // 하단패널 (모듈 미리보기)
            Rect bottomPanel = new Rect(0, position.height - bottomPanelHeight, 
                                     position.width, bottomPanelHeight);
            DrawModulePreview(bottomPanel);
            
            // 하단 툴바 (저장/불러오기)
            Rect toolbarRect = new Rect(0, position.height - 20, position.width, 20);
            DrawBottomPanel(toolbarRect);
        }
        catch (Exception e)
        {
            // GUILayout 오류 발생 시 콘솔에 로그 출력
            Debug.LogError("GUILayout Error: " + e.Message + "\n" + e.StackTrace);
            
            // 모든 GUILayout 상태 리셋 - 잘못된 메서드 호출 수정
            try {
                // 열려 있을 수 있는 모든 GUILayout 그룹 종료
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            catch {
                // 이미 닫혀있는 그룹에 대한 예외는 무시
            }
            
            EditorGUIUtility.ExitGUI();
        }
    }

    private void DrawModuleSelectionPanel(Rect topPanel)
    {
        // 패널 그리기
        GUILayout.BeginArea(topPanel);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
        
        // 모듈 카테고리별 정렬
        var modulesByCategory = new Dictionary<RoomModule.ModuleCategory, List<RoomModule>>();
        foreach (var module in availableModules)
        {
            if (module == null) continue;
            
            if (!modulesByCategory.ContainsKey(module.category))
            {
                modulesByCategory[module.category] = new List<RoomModule>();
            }
            modulesByCategory[module.category].Add(module);
        }
        
        // 각 카테고리 표시
        foreach (var category in System.Enum.GetValues(typeof(RoomModule.ModuleCategory)))
        {
            RoomModule.ModuleCategory cat = (RoomModule.ModuleCategory)category;
            if (!modulesByCategory.ContainsKey(cat)) continue;
            
            // 카테고리 제목 표시
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(cat.ToString(), EditorStyles.boldLabel);
            
            // 카테고리별 모듈 표시
            GUILayout.BeginHorizontal();
            int count = 0;
            
            // 안전을 위해 해당 카테고리의 모듈이 없으면 가로 레이아웃 종료
            if (modulesByCategory[cat].Count == 0)
            {
                GUILayout.EndHorizontal();
                continue;
            }
            
            foreach (var module in modulesByCategory[cat])
            {
                // 4열 간격으로 모듈 버튼 표시
                if (count > 0 && count % 4 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                
                // 모듈 버튼 색상 설정
                Color backgroundColor = GUI.backgroundColor;
                if (selectedModule == module)
                {
                    GUI.backgroundColor = Color.yellow; // 선택된 모듈 강조
                }
                else
                {
                    // 카테고리에 따른 색상 지정
                    GUI.backgroundColor = GetModuleCategoryColor(module.category);
                }
                
                GUILayout.BeginVertical(GUILayout.Width(75));
                
                // 썸네일 또는 색상 블록으로 모듈 표시
                Rect buttonRect = GUILayoutUtility.GetRect(70, 70);
                
                // 색상 블록과 라벨로 표시
                GUI.Box(buttonRect, "");
                
                // 모듈 이름 표시
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.wordWrap = true;
                
                Rect labelRect = new Rect(buttonRect.x, buttonRect.y + 20, buttonRect.width, buttonRect.height - 40);
                GUI.Label(labelRect, module.name, nameStyle);
                
                // 버튼 처리
                if (GUI.Button(buttonRect, "", GUIStyle.none))
                {
                    // 모듈 선택 시 배치 모드 시작
                    selectedModule = module;
                    isPlacingModule = true;
                }
                
                GUILayout.EndVertical();
                
                // 배경색 복원
                GUI.backgroundColor = backgroundColor;
                
                count++;
            }
            
            // 반드시 시작한 가로 레이아웃을 종료
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        
        // 스타일 초기화 (필요 시)
        if (moduleButtonStyle == null)
        {
            moduleButtonStyle = new GUIStyle(GUI.skin.button);
            moduleButtonStyle.padding = new RectOffset(0, 0, 0, 0);
            moduleButtonStyle.margin = new RectOffset(4, 4, 4, 4);
        }
    }

    private void DrawMapEditingPanel()
    {
        EditorGUILayout.BeginVertical();

        // 맵 컨트롤 영역
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Reset View", EditorStyles.toolbarButton))
        {
            zoomLevel = 1.0f;
            mapOffset = Vector2.zero;
        }

        zoomLevel = EditorGUILayout.Slider("Zoom", zoomLevel, 0.1f, 3.0f, GUILayout.Width(200));

        // Ultra Simple Mode 토글 버튼 (빠른 접근용)
        GUI.color = ultraSimpleMode ? Color.green : Color.white;
        if (GUILayout.Button("Ultra Simple Mode", EditorStyles.toolbarButton))
        {
            ultraSimpleMode = !ultraSimpleMode;
            if (ultraSimpleMode)
            {
                // 초극한 단순화 모드 - 모든 기능 최소화
                showConnectionPoints = false;
                enableDynamicRendering = true;
                renderQuality = 0;
                repaintInterval = 0.3f; // 약간 더 빠른 갱신 (3.33fps)
                emergencyMode = true;
            }
            else
            {
                // 원래 상태로 복원
                repaintInterval = DEFAULT_REPAINT_INTERVAL;
                emergencyMode = false;
            }
            
            // 모드 전환 시 강제 화면 갱신 및 타임스탬프 리셋
            lastRepaintTime = 0f;
            lastModeChangeTime = (float)EditorApplication.timeSinceStartup;
            EditorUtility.SetDirty(this);
            this.Repaint();
            SceneView.RepaintAll();
        }
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        if (selectedModule != null)
        {
            EditorGUILayout.LabelField("Selected: " + selectedModule.name);
            
            if (GUILayout.Button("Rotate", EditorStyles.toolbarButton))
            {
                // 선택된 모듈 회전 (프리뷰용)
            }
            
            if (GUILayout.Button("Cancel", EditorStyles.toolbarButton))
            {
                selectedModule = null;
                isPlacingModule = false;
            }
        }
        else if (selectedPlacedModule != null)
        {
            EditorGUILayout.LabelField("Selected: " + selectedPlacedModule.moduleData.name);
            
            if (GUILayout.Button("Rotate", EditorStyles.toolbarButton))
            {
                selectedPlacedModule.rotationStep = (selectedPlacedModule.rotationStep + 1) % 4;
                Repaint();
            }
            
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
            {
                // 연결된 모듈에서 이 모듈과의 연결 제거
                RemoveModuleConnections(selectedPlacedModule);
                
                // 모듈 제거
                placedModules.Remove(selectedPlacedModule);
                selectedPlacedModule = null;
                Repaint();
            }
            
            if (GUILayout.Button("Deselect", EditorStyles.toolbarButton))
            {
                selectedPlacedModule = null;
            }
        }
        
        EditorGUILayout.EndHorizontal();

        // 맵 편집 영역
        Rect mapRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawMap(mapRect);

        EditorGUILayout.EndVertical();
    }

    private void DrawMap(Rect mapRect)
    {
        // 항상 그리기 (프레임 제한 제거)
        lastDrawTime = (float)EditorApplication.timeSinceStartup;
        
        // 맵 배경
        GUI.Box(mapRect, "");
        
        // 맵 원점 계산
        Vector2 mapCenter = new Vector2(mapRect.x + mapRect.width / 2, mapRect.y + mapRect.height / 2);
        Vector2 viewportOrigin = mapCenter + mapOffset;
        
        // 그리드 그리기
        DrawGrid(mapRect, viewportOrigin);
        
        // 초극한 단순화 모드에서는 기능 제한
        if (ultraSimpleMode)
        {
            // 가시 영역 계산
            Rect visibleArea = GetVisibleWorldRect(mapRect);
            
            // 지금 보이는 모듈만 그리기
            foreach (var module in placedModules)
            {
                if (IsModuleVisible(module, visibleArea))
                {
                    DrawModule(mapRect, module);
                }
            }
            
            return; // 초극한 단순화 모드에서는 여기서 종료
        }
        
        // 빠른 모드 처리
        if (emergencyMode)
        {
            // 가시 영역 계산
            Rect visibleArea = GetVisibleWorldRect(mapRect);
            
            // 임시 리스트에 보이는 모듈만 저장 (LINQ 대신 직접 필터링)
            List<PlacedModule> visibleModules = new List<PlacedModule>();
            foreach (var module in placedModules)
            {
                if (IsModuleVisible(module, visibleArea))
                {
                    visibleModules.Add(module);
                }
            }
            
            // 보이는 모듈만 그리기
            foreach (var module in visibleModules)
            {
                DrawModule(mapRect, module);
            }
            
            // 연결선 그리기 (단순화)
            DrawSimplifiedConnections(visibleModules, mapRect);
        }
        else
        {
            // 표준 모드: 모든 모듈 그리기
            foreach (PlacedModule module in placedModules)
            {
                DrawModule(mapRect, module);
            }
            
            // 모든 연결선 그리기
            DrawConnections(mapRect);
        }
        
        // 현재 선택된 모듈이 있고 배치 중이면 팔로워로 그리기
        if (selectedModule != null)
        {
            Vector2 worldPos = GetWorldPosition(Event.current.mousePosition, mapRect);
            // 그리드 스냅
            worldPos.x = Mathf.Round(worldPos.x / GRID_SIZE) * GRID_SIZE;
            worldPos.y = Mathf.Round(worldPos.y / GRID_SIZE) * GRID_SIZE;
            
            DrawFollowerModule(selectedModule, worldPos, mapRect);
        }
        
        // 연결점 드래그 중이면 연결 라인 그리기
        if (currentState == EditorState.DraggingConnection && draggingModule != null && draggingConnectionIndex >= 0)
        {
            ConnectionPoint sourcePoint = draggingModule.moduleData.connectionPoints[draggingConnectionIndex];
            Vector2 rotatedSourcePoint = RotatePoint(sourcePoint.position, Vector2.zero, 
                draggingModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 sourceWorldPos = draggingModule.position + rotatedSourcePoint;
            Vector2 worldPos = GetWorldPosition(Event.current.mousePosition, mapRect);
            
            DrawConnectionLine(sourceWorldPos, worldPos, sourcePoint.type, mapRect);
        }
    }

    private bool IsModuleVisible(PlacedModule module, Rect worldViewRect)
    {
        // 모듈 크기를 고려한 경계 박스 계산
        float moduleSize = 1.5f; // 모듈 사이즈(여유 있게)
        Rect moduleRect = new Rect(
            module.position.x - moduleSize, 
            module.position.y - moduleSize,
            moduleSize * 2, 
            moduleSize * 2
        );
        
        // 화면에 보이는지 확인
        return worldViewRect.Overlaps(moduleRect);
    }

    private void DrawGrid(Rect mapRect, Vector2 origin)
    {
        // 여기에 그리드 그리기 코드 구현
        GUI.Box(mapRect, "", EditorStyles.helpBox);
        
        // 렌더링 품질이 낮으면 그리드를 생략
        if (renderQuality == 0 && zoomLevel < 0.5f)
            return;
        
        // 타일 그리드 또는 일반 그리드 선택
        float gridSizeX = useRoomTileGrid ? TILE_WIDTH : GRID_SIZE;
        float gridSizeY = useRoomTileGrid ? TILE_HEIGHT : GRID_SIZE;
        
        // 줌 레벨에 따라 그리드 표시 여부 결정
        if (zoomLevel < 0.3f && useRoomTileGrid)
        {
            // 매우 줌아웃된 상태에서는 타일 그리드 대신 표준 룸 크기 단위로 그리드 표시
            gridSizeX = RoomModule.STANDARD_ROOM_WIDTH_TILES * TILE_WIDTH;
            gridSizeY = RoomModule.STANDARD_ROOM_HEIGHT_TILES * TILE_HEIGHT;
        }
        else if (zoomLevel < 0.1f)
        {
            // 극단적인 줌아웃에서는 그리드를 표시하지 않음
            return;
        }
        
        // 그리드 크기 설정 - 줌 레벨에 따라 스케일링
        float scaledGridX = gridSizeX * zoomLevel;
        float scaledGridY = gridSizeY * zoomLevel;
        
        // 그리드 오프셋 계산
        float offsetX = mapOffset.x % scaledGridX;
        float offsetY = mapOffset.y % scaledGridY;
        
        // 그리드 투명도 설정 - 줌 레벨에 따라 조정
        float gridAlpha = 0.3f;
        if (zoomLevel < 0.5f)
            gridAlpha = 0.15f;
        else if (zoomLevel > 1.5f)
            gridAlpha = 0.4f;
        
        Color gridColor = new Color(0.5f, 0.5f, 0.5f, gridAlpha);
        Color roomColor = new Color(0.0f, 0.5f, 0.7f, gridAlpha + 0.1f);
        
        // 숫자가 많아서 5칸 단위로 강조하는 색상
        Color emphasisColor = new Color(0.3f, 0.6f, 0.3f, gridAlpha + 0.05f);
        
        // 가로 그리드 라인
        int lineCount = Mathf.CeilToInt(mapRect.height / scaledGridY) + 1;
        for (int i = 0; i < lineCount; i++)
        {
            float y = offsetY + i * scaledGridY;
            if (y >= 0 && y <= mapRect.height)
            {
                // 룸 타일 그리드일 경우 표준 룸 크기 단위마다 다른 색상 적용
                bool isRoomBoundary = useRoomTileGrid && 
                    (i % RoomModule.STANDARD_ROOM_HEIGHT_TILES == 0) && 
                    zoomLevel >= 0.3f;
                    
                // 5칸 단위로 색상 강조
                bool isFiveMultiple = i % 5 == 0 && !isRoomBoundary;
                
                Handles.color = isRoomBoundary ? roomColor : (isFiveMultiple ? emphasisColor : gridColor);
                float lineThickness = isRoomBoundary ? 2f : (isFiveMultiple ? 1.5f : 1f);
                
                if (isRoomBoundary || isFiveMultiple)
                {
                    Handles.DrawLine(
                        new Vector3(mapRect.x, mapRect.y + y), 
                        new Vector3(mapRect.x + mapRect.width, mapRect.y + y),
                        lineThickness
                    );
                }
                else
                {
                Handles.DrawLine(
                    new Vector3(mapRect.x, mapRect.y + y), 
                    new Vector3(mapRect.x + mapRect.width, mapRect.y + y)
                );
                }
            }
        }
        
        // 세로 그리드 라인
        lineCount = Mathf.CeilToInt(mapRect.width / scaledGridX) + 1;
        for (int i = 0; i < lineCount; i++)
        {
            float x = offsetX + i * scaledGridX;
            if (x >= 0 && x <= mapRect.width)
            {
                // 룸 타일 그리드일 경우 표준 룸 크기 단위마다 다른 색상 적용
                bool isRoomBoundary = useRoomTileGrid && 
                    (i % RoomModule.STANDARD_ROOM_WIDTH_TILES == 0) && 
                    zoomLevel >= 0.3f;
                    
                // 5칸 단위로 색상 강조
                bool isFiveMultiple = i % 5 == 0 && !isRoomBoundary;
                
                Handles.color = isRoomBoundary ? roomColor : (isFiveMultiple ? emphasisColor : gridColor);
                float lineThickness = isRoomBoundary ? 2f : (isFiveMultiple ? 1.5f : 1f);
                
                if (isRoomBoundary || isFiveMultiple)
                {
                    Handles.DrawLine(
                        new Vector3(mapRect.x + x, mapRect.y), 
                        new Vector3(mapRect.x + x, mapRect.y + mapRect.height),
                        lineThickness
                    );
                }
                else
                {
                Handles.DrawLine(
                    new Vector3(mapRect.x + x, mapRect.y), 
                    new Vector3(mapRect.x + x, mapRect.y + mapRect.height)
                );
                }
            }
        }
    }

    private void DrawModule(Rect mapRect, PlacedModule module)
    {
        Vector2 screenPos = GetScreenPosition(module.position, mapRect);
        
        // 모듈 위치가 화면 범위를 벗어나면 그리지 않음
        if (screenPos.x < -100 || screenPos.x > mapRect.width + 100 || 
            screenPos.y < -100 || screenPos.y > mapRect.height + 100)
            return;
        
        float width, height;
        
        // 모듈 크기 적용
        Vector2 moduleSize = module.moduleData.moduleSize;
        
        // 초극한 단순화 모드
        if (ultraSimpleMode)
        {
            // 가장 단순한 형태로 표시 - 텍스트만 표시
            width = 30f * zoomLevel; // 작은 사이즈로
            height = 30f * zoomLevel;
            Rect simpleRect = new Rect(screenPos.x - width/2, screenPos.y - height/2, width, height);
            
            // 단순 컬러 블록으로 표시
            Color fillColor = Color.gray;
            Color borderColor = (module == selectedPlacedModule) ? Color.yellow : Color.white;
            
            Handles.DrawSolidRectangleWithOutline(simpleRect, fillColor, borderColor);
            
            // 최소한의 텍스트 정보만 표시
            GUIStyle simpleLabelStyle = new GUIStyle(GUI.skin.label);
            simpleLabelStyle.alignment = TextAnchor.MiddleCenter;
            simpleLabelStyle.normal.textColor = Color.white;
            
            GUI.Label(simpleRect, module.moduleData.name, simpleLabelStyle);
            
            return;
        }
        
        // 간소화된 모드 및 응급 모드
        if (emergencyMode || (zoomLevel < 0.5f && renderQuality < 1))
        {
            // 심플 모드 - 썸네일 없이 간단한 사각형만 그리기
            width = moduleSize.x * 25f * zoomLevel;
            height = moduleSize.y * 25f * zoomLevel;
            
            // 회전 단계에 따라 가로/세로 치환
            if (module.rotationStep % 2 == 1) // 90도 또는 270도 회전
            {
                float temp = width;
                width = height;
                height = temp;
            }
            
            Rect simpleRect = new Rect(screenPos.x - width/2, screenPos.y - height/2, width, height);
            
            Color fillColor = GetModuleCategoryColor(module.moduleData.category);
            
            Color borderColor = (module == selectedPlacedModule) ? Color.yellow : Color.white;
            
            Handles.DrawSolidRectangleWithOutline(
                new Rect(simpleRect.x, simpleRect.y, simpleRect.width, simpleRect.height),
                fillColor, borderColor
            );
            
            // 모듈 이름 표시
            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.normal.textColor = Color.white;
            nameStyle.fontStyle = FontStyle.Bold;
            GUI.Label(simpleRect, module.moduleData.name, nameStyle);
            
            // 응급 모드에서는 연결점 표시 생략
            if (!emergencyMode && showConnectionPoints)
            {
                // 최대 2개의 연결점만 간단하게 표시
                int pointsToShow = Mathf.Min(module.moduleData.connectionPoints.Length, 2);
                for (int i = 0; i < pointsToShow; i++)
                {
                    Vector2 pointPos = RotatePoint(module.moduleData.connectionPoints[i].position, Vector2.zero, 
                        module.rotationStep * 90 * Mathf.Deg2Rad);
                    Vector2 connectionScreenPos = screenPos + pointPos * zoomLevel;
                    Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, 2f);
                }
            }
            
            return;
        }
        
        // 모듈 크기 기반으로 렌더링 크기 조정
        width = moduleSize.x * 50 * zoomLevel;
        height = moduleSize.y * 50 * zoomLevel;
        
        // 회전 단계에 따라 가로/세로 치환
        if (module.rotationStep % 2 == 1) // 90도 또는 270도 회전
        {
            float temp = width;
            width = height;
            height = temp;
        }
        
        Rect moduleRect = new Rect(screenPos.x - width / 2, screenPos.y - height / 2, width, height);
        
        // 모듈 그리기
        GUI.color = (module == selectedPlacedModule) ? Color.yellow : Color.white;
        Matrix4x4 matrixBackup = GUI.matrix;
        
        // 모듈 회전 처리
        if (module.rotationStep > 0)
        {
            GUIUtility.RotateAroundPivot(module.rotationStep * 90, screenPos);
        }
        
        // 썸네일 또는 컬러 박스로 표시
        Color moduleColor = GetModuleCategoryColor(module.moduleData.category);
        
        // 썸네일 대신 컬러 박스로 표시
        Handles.DrawSolidRectangleWithOutline(moduleRect, moduleColor, Color.white);
        
        // 모듈 이름 표시 (회전이 적용된 상태)
        GUIStyle moduleLabelStyle = new GUIStyle(GUI.skin.label);
        moduleLabelStyle.alignment = TextAnchor.MiddleCenter;
        moduleLabelStyle.normal.textColor = Color.white;
        moduleLabelStyle.fontStyle = FontStyle.Bold;
        
        // 모듈 이름 표시
        GUI.Label(moduleRect, module.moduleData.name, moduleLabelStyle);
        
        // 테마 정보 표시
        GUIStyle themeStyle = new GUIStyle(GUI.skin.label);
        themeStyle.alignment = TextAnchor.LowerCenter;
        themeStyle.normal.textColor = Color.white;
        themeStyle.fontSize = Mathf.Max(9, (int)(10 * zoomLevel));
        
        Rect themeRect = new Rect(moduleRect.x, moduleRect.y + moduleRect.height * 0.7f, 
                                 moduleRect.width, moduleRect.height * 0.3f);
        GUI.Label(themeRect, module.moduleData.theme.ToString(), themeStyle);
        
        // 연결점 그리기 - 렌더링 품질 설정에 따라 표시
        if (showConnectionPoints && module.moduleData.connectionPoints != null)
        {
            // 렌더링 품질에 따라 그릴 연결점 수 결정
            int connectionPointsToRender = module.moduleData.connectionPoints.Length;
            if (renderQuality == 0 && connectionPointsToRender > 4)
                connectionPointsToRender = 4; // 낮은 품질에서는 최대 4개만 그림
            
            for (int i = 0; i < connectionPointsToRender; i++)
            {
                ConnectionPoint point = module.moduleData.connectionPoints[i];
                Vector2 pointPos = RotatePoint(point.position, Vector2.zero, module.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 connectionScreenPos = screenPos + pointPos * zoomLevel;
                
                // 작은 사이즈로 표시할지 결정
                float pointSize = 5f;
                if (renderQuality < 2) // 낮거나 중간 품질
                    pointSize = 3f;
                
                // 연결점 표시
                Handles.color = GetConnectionTypeColor(point.type);
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, pointSize);
            }
        }
        
        // GUI 매트릭스 복원
        GUI.matrix = matrixBackup;
        GUI.color = Color.white;
    }

    // 모듈 카테고리에 따른 색상 반환 함수 추가
    private Color GetModuleCategoryColor(RoomModule.ModuleCategory category)
    {
        switch (category)
        {
            case RoomModule.ModuleCategory.Combat:
                return new Color(1f, 0.6f, 0.6f, 0.7f); // 붉은색
            case RoomModule.ModuleCategory.Puzzle:
                return new Color(0.6f, 0.8f, 1f, 0.7f); // 푸른색
            case RoomModule.ModuleCategory.Boss:
                return new Color(1f, 0.2f, 0.2f, 0.7f); // 진한 빨강
            case RoomModule.ModuleCategory.Hub:
                return new Color(0.8f, 0.8f, 0.3f, 0.7f); // 노란색
            case RoomModule.ModuleCategory.Corridor:
                return new Color(0.7f, 0.7f, 0.7f, 0.7f); // 회색
            case RoomModule.ModuleCategory.Village:
                return new Color(0.6f, 1f, 0.6f, 0.7f); // 녹색
            case RoomModule.ModuleCategory.Save:
                return new Color(0.6f, 1f, 1f, 0.7f); // 청록색
            case RoomModule.ModuleCategory.Secret:
                return new Color(0.8f, 0.4f, 1f, 0.7f); // 보라색
            default:
                return new Color(0.8f, 0.8f, 0.8f, 0.7f); // 기본 회색
        }
    }

    private Color GetConnectionTypeColor(ConnectionPoint.ConnectionType type)
    {
        switch (type)
        {
            case ConnectionPoint.ConnectionType.Normal:
                return Color.green;
            case ConnectionPoint.ConnectionType.OneWay:
                return Color.yellow;
            case ConnectionPoint.ConnectionType.LockedDoor:
                return Color.red;
            case ConnectionPoint.ConnectionType.AbilityGate:
                return Color.blue;
            default:
                return Color.white;
        }
    }

    private void DrawConnections(Rect mapRect, List<PlacedModule> modules)
    {
        // 응급 모드에서는 간소화된 연결선만 그리기
        if (emergencyMode)
        {
            foreach (var module in modules)
            {
                if (module == null || module.moduleData == null || module.connections == null)
                    continue;
                    
                Vector2 from = GetScreenPosition(module.position, mapRect);
                
                foreach (var connection in module.connections)
                {
                    if (connection == null || connection.connectedModule == null || 
                        connection.connectedModule.moduleData == null)
                        continue;
                    
                    // 이미 처리한 연결은 건너뛰기 (중복 방지) - LINQ 제거
                    bool skip = false;
                    for (int i = 0; i < modules.Count; i++)
                    {
                        if (modules[i] == connection.connectedModule)
                        {
                            for (int j = 0; j < modules.Count; j++)
                            {
                                if (modules[j] == module && j > i)
                                {
                                    skip = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    
                    if (skip) continue;
                    
                    Vector2 to = GetScreenPosition(connection.connectedModule.position, mapRect);
                    
                    Handles.color = new Color(0.2f, 0.8f, 0.8f, 0.5f);
                    Handles.DrawLine(from, to);
                }
            }
            return;
        }
        
        // 정상 모드 연결선 처리 - 성능 최적화
        // 연결선 일괄 처리를 위한 리스트
        List<Vector3> linePoints = new List<Vector3>();
        int maxConnections = emergencyMode ? 100 : 500; // 최대 연결선 수 제한
        int connectionCount = 0;
        
        for (int i = 0; i < modules.Count; i++)
        {
            PlacedModule module = modules[i];
            if (module == null || module.moduleData == null || module.connections == null)
                continue;
                
            for (int c = 0; c < module.connections.Count; c++)
            {
                var connection = module.connections[c];
                if (connection == null || connection.connectedModule == null || 
                    connection.connectedModule.moduleData == null)
                    continue;
                
                // 이미 처리한 연결은 건너뛰기 (중복 방지) - LINQ 제거
                bool skip = false;
                for (int j = 0; j < modules.Count; j++)
                {
                    if (modules[j] == connection.connectedModule)
                    {
                        if (j < i) // 이미 처리된 모듈
                        {
                            skip = true;
                        }
                        break;
                    }
                }
                
                if (skip) continue;
                    
                // 소스 모듈의 연결점 위치 계산
                if (connection.connectionPointIndex >= module.moduleData.connectionPoints.Length)
                    continue;
                    
                ConnectionPoint sourcePoint = module.moduleData.connectionPoints[connection.connectionPointIndex];
                Vector2 rotatedSourcePoint = RotatePoint(sourcePoint.position, Vector2.zero, module.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 sourceWorldPos = module.position + rotatedSourcePoint;
                Vector2 sourceScreenPos = GetScreenPosition(sourceWorldPos, mapRect);
                
                // 타겟 모듈의 연결점 위치 계산
                if (connection.connectedPointIndex >= connection.connectedModule.moduleData.connectionPoints.Length)
                    continue;
                    
                ConnectionPoint targetPoint = connection.connectedModule.moduleData.connectionPoints[connection.connectedPointIndex];
                Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, connection.connectedModule.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 targetWorldPos = connection.connectedModule.position + rotatedTargetPoint;
                Vector2 targetScreenPos = GetScreenPosition(targetWorldPos, mapRect);
                
                // 연결선 포인트 추가
                linePoints.Add(new Vector3(sourceScreenPos.x, sourceScreenPos.y, 0));
                linePoints.Add(new Vector3(targetScreenPos.x, targetScreenPos.y, 0));
                
                connectionCount++;
                if (connectionCount >= maxConnections)
                    break;
            }
            
            if (connectionCount >= maxConnections)
                break;
        }
        
        // 한 번에 모든 연결선 그리기
        if (linePoints.Count > 0)
        {
            Handles.color = Color.cyan;
            Handles.DrawLines(linePoints.ToArray());
        }
    }

    private void DrawModulePreview(Rect bottomPanel)
    {
        EditorGUILayout.BeginVertical(GUILayout.Height(bottomPanel.height));
        
        EditorGUILayout.LabelField("Module Preview", EditorStyles.boldLabel);
        
        if (selectedPlacedModule != null)
        {
            EditorGUILayout.LabelField("Selected Module: " + selectedPlacedModule.moduleData.name);
            
            // 모듈 속성 표시
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Category:", selectedPlacedModule.moduleData.category);
            EditorGUILayout.EnumPopup("Theme:", selectedPlacedModule.moduleData.theme);
            EditorGUILayout.Vector2Field("Size:", selectedPlacedModule.moduleData.moduleSize);
            EditorGUILayout.Toggle("Special Room:", selectedPlacedModule.moduleData.isSpecialRoom);
            EditorGUI.EndDisabledGroup();
            
            // 회전 컨트롤
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate CW"))
            {
                selectedPlacedModule.rotationStep = (selectedPlacedModule.rotationStep + 1) % 4;
                Repaint();
            }
            if (GUILayout.Button("Rotate CCW"))
            {
                selectedPlacedModule.rotationStep = (selectedPlacedModule.rotationStep + 3) % 4; // +3 = -1 mod 4
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            // 모듈 위치 표시
            EditorGUILayout.Vector2Field("Position:", selectedPlacedModule.position);
            
            // 모듈 삭제
            if (GUILayout.Button("Delete Module"))
            {
            RemoveModuleConnections(selectedPlacedModule);
            placedModules.Remove(selectedPlacedModule);
            selectedPlacedModule = null;
            Repaint();
            }
        }
        else if (selectedModule != null)
        {
            EditorGUILayout.LabelField("Placing Module: " + selectedModule.name);
            
            // 모듈 속성 표시
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Category:", selectedModule.category);
            EditorGUILayout.EnumPopup("Theme:", selectedModule.theme);
            EditorGUILayout.Vector2Field("Size:", selectedModule.moduleSize);
            EditorGUILayout.Toggle("Special Room:", selectedModule.isSpecialRoom);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.LabelField("Click on the map to place the module.");
            
            if (GUILayout.Button("Cancel Placement"))
            {
                CancelModulePlacement();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No module selected.");
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawBottomPanel(Rect toolbarRect)
    {
        // GUI 직접 호출로 변경 (GUILayout 대신)
        GUI.Box(toolbarRect, "", EditorStyles.toolbar);
        
        float buttonWidth = 80;
        float spacing = 5;
        float labelWidth = 70;
        float textFieldWidth = 150;
        
        // 맵 이름 입력
        GUI.Label(new Rect(toolbarRect.x + 5, toolbarRect.y + 2, labelWidth, 16), "Map Name:");
        mapName = GUI.TextField(new Rect(toolbarRect.x + 5 + labelWidth, toolbarRect.y + 2, textFieldWidth, 16), mapName);
        
        // 그리드 정보 표시 개선
        float gridInfoX = toolbarRect.x + 5 + labelWidth + textFieldWidth + 20;
        GUI.Label(new Rect(gridInfoX, toolbarRect.y + 2, 250, 16), 
            string.Format("그리드: 1 모듈 타일 = {0} 유니티 타일 | 룸: {1}x{2}", 
            RoomModule.UNITY_TILES_PER_MODULE_TILE,
            RoomModule.STANDARD_ROOM_WIDTH_TILES, 
            RoomModule.STANDARD_ROOM_HEIGHT_TILES));
        
        // 버튼 시작 위치
        float startX = toolbarRect.width - (buttonWidth * 5 + spacing * 4);
        
        // 설정 버튼 추가
        if (GUI.Button(new Rect(startX, toolbarRect.y, buttonWidth, 20), "Settings", EditorStyles.toolbarButton))
        {
            ShowSettingsMenu();
        }
        
        // 자동 맵 생성 버튼 추가
        if (GUI.Button(new Rect(startX + buttonWidth + spacing, toolbarRect.y, buttonWidth, 20), "Auto Generate", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("Auto Generate Map", "Generate a procedural map?", "Yes", "Cancel"))
            {
                AutoGenerateMap();
            }
        }
        
        // 맵 저장 버튼
        if (GUI.Button(new Rect(startX + (buttonWidth + spacing) * 2, toolbarRect.y, buttonWidth, 20), "Save Map", EditorStyles.toolbarButton))
        {
            SaveMap();
        }
        
        // 맵 불러오기 버튼
        if (GUI.Button(new Rect(startX + (buttonWidth + spacing) * 3, toolbarRect.y, buttonWidth, 20), "Load Map", EditorStyles.toolbarButton))
        {
            LoadMap();
        }
        
        // 새 맵 버튼
        if (GUI.Button(new Rect(startX + (buttonWidth + spacing) * 4, toolbarRect.y, buttonWidth, 20), "New Map", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("New Map", "Are you sure you want to clear the current map?", "Yes", "Cancel"))
            {
                NewMap();
            }
        }
    }
    
    private void ShowSettingsMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Dynamic Rendering"), enableDynamicRendering, () => {
            enableDynamicRendering = !enableDynamicRendering;
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Show Connection Points"), showConnectionPoints, () => {
            showConnectionPoints = !showConnectionPoints;
            Repaint();
        });
        
        menu.AddSeparator("");
        
        // 그리드 타입 설정 추가
        menu.AddItem(new GUIContent("Use Tile Grid"), useRoomTileGrid, () => {
            useRoomTileGrid = !useRoomTileGrid;
            Repaint();
        });
        
        // 모든 모듈에 표준 크기 적용 메뉴 아이템 추가
        menu.AddItem(new GUIContent("Set Standard Size To All Modules"), false, () => {
            if (EditorUtility.DisplayDialog("표준 크기 적용", 
                "모든 모듈에 표준 룸 크기(가로 " + RoomModule.STANDARD_ROOM_WIDTH_TILES + "×" + 
                TILE_WIDTH + ", 세로 " + RoomModule.STANDARD_ROOM_HEIGHT_TILES + "×" + 
                TILE_HEIGHT + ")를 적용하시겠습니까?", 
                "예", "아니오"))
            {
                RoomModule.SetStandardSizeToAllModules();
                Repaint();
            }
        });
        
        // 커스텀 크기 모듈 생성 메뉴 추가
        menu.AddItem(new GUIContent("Create Custom Size Module"), false, () => {
            ShowCustomSizeModuleWindow();
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("Quality/Low"), renderQuality == 0, () => {
            renderQuality = 0;
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Quality/Medium"), renderQuality == 1, () => {
            renderQuality = 1;
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Quality/High"), renderQuality == 2, () => {
            renderQuality = 2;
            Repaint();
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("Emergency Mode"), emergencyMode, () => {
            emergencyMode = !emergencyMode;
            if (emergencyMode)
            {
                // 극단적 성능 모드
                showConnectionPoints = false;
                enableDynamicRendering = true;
                renderQuality = 0;
                repaintInterval = 0.2f; // 5fps로 제한
            }
            else
            {
                // 원래 상태로 복원
                repaintInterval = DEFAULT_REPAINT_INTERVAL;
            }
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Ultra Simple Mode"), ultraSimpleMode, () => {
            ultraSimpleMode = !ultraSimpleMode;
            if (ultraSimpleMode)
            {
                // 초극한 단순화 모드 - 모든 기능 최소화
                showConnectionPoints = false;
                enableDynamicRendering = true;
                renderQuality = 0;
                repaintInterval = 0.3f; // 약간 더 빠른 갱신 (3.33fps)
                emergencyMode = true;
            }
            else
            {
                // 원래 상태로 복원
                repaintInterval = DEFAULT_REPAINT_INTERVAL;
                emergencyMode = false;
            }
            
            // 모드 전환 시 강제 화면 갱신 및 타임스탬프 리셋 
            lastRepaintTime = 0f;
            lastModeChangeTime = (float)EditorApplication.timeSinceStartup;
            EditorUtility.SetDirty(this);
            this.Repaint();
            SceneView.RepaintAll();
        });
        
        menu.ShowAsContext();
    }
    
    // 커스텀 사이즈 모듈 생성 창 표시
    private void ShowCustomSizeModuleWindow()
    {
        CustomSizeModuleWindow window = EditorWindow.GetWindow<CustomSizeModuleWindow>("커스텀 사이즈 모듈 생성");
        window.minSize = new Vector2(350, 250);
        window.maxSize = new Vector2(400, 350);
        window.Show();
    }
    
    // 커스텀 사이즈 모듈 생성 윈도우 클래스
    public class CustomSizeModuleWindow : EditorWindow
    {
        private string moduleName = "New Module";
        private int widthInTiles = 4;
        private int heightInTiles = 2;
        private RoomModule.ModuleCategory category = RoomModule.ModuleCategory.Combat;
        private RoomModule.EnvironmentTheme theme = RoomModule.EnvironmentTheme.Aether_Dome;
        private GameObject modulePrefab;
        
        private void OnGUI()
        {
            GUILayout.Label("커스텀 사이즈 모듈 생성", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            moduleName = EditorGUILayout.TextField("모듈 이름", moduleName);
            modulePrefab = (GameObject)EditorGUILayout.ObjectField("모듈 프리팹", modulePrefab, typeof(GameObject), false);
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("모듈 크기 (타일 단위)", EditorStyles.boldLabel);
            widthInTiles = EditorGUILayout.IntSlider("가로 타일 수", widthInTiles, 1, 20);
            heightInTiles = EditorGUILayout.IntSlider("세로 타일 수", heightInTiles, 1, 20);
            
            EditorGUILayout.Space(5);
            
            GUILayout.Label(string.Format("모듈 크기: {0}x{1} (유니티 타일: {2}x{3})", 
                widthInTiles, 
                heightInTiles,
                widthInTiles * RoomModule.UNITY_TILES_PER_MODULE_TILE, 
                heightInTiles * RoomModule.UNITY_TILES_PER_MODULE_TILE));
                
            EditorGUILayout.Space(10);
            
            category = (RoomModule.ModuleCategory)EditorGUILayout.EnumPopup("카테고리", category);
            theme = (RoomModule.EnvironmentTheme)EditorGUILayout.EnumPopup("테마", theme);
            
            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("생성", GUILayout.Height(30)))
            {
                CreateCustomSizeModule();
                this.Close();
            }
        }
        
        private void CreateCustomSizeModule()
        {
            // 저장 경로 확인 및 생성
            string modulePath = "Assets/03_Scripts/RoomModules/";
            if (!System.IO.Directory.Exists(modulePath))
            {
                System.IO.Directory.CreateDirectory(modulePath);
            }

            // 스크립터블 오브젝트 생성
            RoomModule moduleAsset = ScriptableObject.CreateInstance<RoomModule>();
            moduleAsset.modulePrefab = modulePrefab;
            moduleAsset.category = category;
            moduleAsset.theme = theme;
            moduleAsset.isSpecialRoom = false;
            
            // 커스텀 크기 설정
            moduleAsset.SetSizeByTiles(widthInTiles, heightInTiles);
            
            // 기본 연결점 추가 (네 방향 한 개씩)
            List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();
            
            // 위쪽 가운데
            connectionPoints.Add(new ConnectionPoint
            {
                position = new Vector2(0, heightInTiles * RoomModule.TILE_HEIGHT / 2),
                direction = ConnectionPoint.ConnectionDirection.Up,
                type = ConnectionPoint.ConnectionType.Normal
            });
            
            // 아래쪽 가운데
            connectionPoints.Add(new ConnectionPoint
            {
                position = new Vector2(0, -heightInTiles * RoomModule.TILE_HEIGHT / 2),
                direction = ConnectionPoint.ConnectionDirection.Down,
                type = ConnectionPoint.ConnectionType.Normal
            });
            
            // 왼쪽 가운데
            connectionPoints.Add(new ConnectionPoint
            {
                position = new Vector2(-widthInTiles * RoomModule.TILE_WIDTH / 2, 0),
                direction = ConnectionPoint.ConnectionDirection.Left,
                type = ConnectionPoint.ConnectionType.Normal
            });
            
            // 오른쪽 가운데
            connectionPoints.Add(new ConnectionPoint
            {
                position = new Vector2(widthInTiles * RoomModule.TILE_WIDTH / 2, 0),
                direction = ConnectionPoint.ConnectionDirection.Right,
                type = ConnectionPoint.ConnectionType.Normal
            });
            
            moduleAsset.connectionPoints = connectionPoints.ToArray();

            // 에셋 저장
            string assetPath = modulePath + moduleName + ".asset";
            AssetDatabase.CreateAsset(moduleAsset, assetPath);
            AssetDatabase.SaveAssets();

            // 에셋 선택
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = moduleAsset;
            
            Debug.Log(string.Format(
                "커스텀 사이즈 모듈이 생성되었습니다: {0} ({1}x{2} 모듈 타일, {3}x{4} 유니티 타일)",
                assetPath, widthInTiles, heightInTiles, 
                widthInTiles * RoomModule.UNITY_TILES_PER_MODULE_TILE,
                heightInTiles * RoomModule.UNITY_TILES_PER_MODULE_TILE
            ));
        }
    }

    // 자동 맵 생성 기능
    private void AutoGenerateMap()
    {
        // 기존 맵 초기화
        NewMap();
        
        // 맵 설정
        int roomCount = EditorUtility.DisplayDialogComplex("Map Generation Settings", 
            "Choose map size:", "Small (5-8)", "Medium (10-15)", "Large (20-30)");
            
        int minRooms, maxRooms;
        switch (roomCount)
        {
            case 0: // Small
                minRooms = 5;
                maxRooms = 8;
                break;
            case 1: // Medium
                minRooms = 10;
                maxRooms = 15;
                break;
            case 2: // Large
                minRooms = 20;
                maxRooms = 30;
                break;
            default:
                minRooms = 5;
                maxRooms = 8;
                break;
        }
        
        // 환경 테마 선택
        RoomModule.EnvironmentTheme selectedTheme = RoomModule.EnvironmentTheme.Aether_Dome;
        string[] themeNames = System.Enum.GetNames(typeof(RoomModule.EnvironmentTheme));
        GenericMenu themeMenu = new GenericMenu();
        
        foreach (RoomModule.EnvironmentTheme theme in System.Enum.GetValues(typeof(RoomModule.EnvironmentTheme)))
        {
            themeMenu.AddItem(new GUIContent(theme.ToString()), false, () => 
            {
                selectedTheme = theme;
                GenerateMapWithSettings(minRooms, maxRooms, selectedTheme);
            });
        }
        
        // 메뉴 표시 - 테마 선택 후 맵 생성
        themeMenu.ShowAsContext();
    }
    
    private void GenerateMapWithSettings(int minRooms, int maxRooms, RoomModule.EnvironmentTheme theme)
    {
        // 실제 생성할 방 개수
        int targetRoomCount = UnityEngine.Random.Range(minRooms, maxRooms + 1);
        
        // 해당 테마에 맞는 모듈만 필터링
        List<RoomModule> themeModules = availableModules.Where(m => m.theme == theme).ToList();
        
        if (themeModules.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No modules available for the selected theme.", "OK");
            return;
        }
        
        // 1. 시작 방 (Hub) 배치
        RoomModule startModule = themeModules.FirstOrDefault(m => m.category == RoomModule.ModuleCategory.Hub);
        if (startModule == null)
        {
            startModule = themeModules[UnityEngine.Random.Range(0, themeModules.Count)];
        }
        
        PlacedModule startRoom = new PlacedModule
        {
            moduleData = startModule,
            position = Vector2.zero,
            rotationStep = 0,
            connections = new List<PlacedModule.ConnectionInfo>()
        };
        
        placedModules.Add(startRoom);
        
        // 2. 나머지 방 재귀적으로 생성
        List<PlacedModule> openRooms = new List<PlacedModule> { startRoom };
        int placedRoomCount = 1;
        
        while (placedRoomCount < targetRoomCount && openRooms.Count > 0)
        {
            // 현재 연결할 방 선택
            int openRoomIndex = UnityEngine.Random.Range(0, openRooms.Count);
            PlacedModule currentRoom = openRooms[openRoomIndex];
            
            // 사용 가능한 연결점 찾기
            List<int> availableConnections = new List<int>();
            for (int i = 0; i < currentRoom.moduleData.connectionPoints.Length; i++)
            {
                bool isConnected = currentRoom.connections.Any(c => c.connectionPointIndex == i);
                if (!isConnected)
                {
                    availableConnections.Add(i);
                }
            }
            
            // 연결점이 모두 사용중이면 openRooms에서 제거
            if (availableConnections.Count == 0)
            {
                openRooms.RemoveAt(openRoomIndex);
                continue;
            }
            
            // 랜덤 연결점 선택
            int connectionIndex = availableConnections[UnityEngine.Random.Range(0, availableConnections.Count)];
            ConnectionPoint connectionPoint = currentRoom.moduleData.connectionPoints[connectionIndex];
            
            // 특수방 결정 (약 20% 확률)
            bool shouldBeSpecial = UnityEngine.Random.value < 0.2f;
            
            // 다음 방 종류 결정 (일반 방, 보스방, 특수방 등)
            RoomModule.ModuleCategory nextCategory;
            if (shouldBeSpecial)
            {
                // 보스방은 맵 끝에만 (현재 방이 1개만 연결된 경우)
                if (placedRoomCount > targetRoomCount * 0.7f && UnityEngine.Random.value < 0.3f)
                {
                    nextCategory = RoomModule.ModuleCategory.Boss;
                }
                else
                {
                    // 다른 특수방 (상점, 아이템 등)
                    var specialCategories = new[] 
                    { 
                        RoomModule.ModuleCategory.Village, 
                        RoomModule.ModuleCategory.Save,
                        RoomModule.ModuleCategory.Secret
                    };
                    nextCategory = specialCategories[UnityEngine.Random.Range(0, specialCategories.Length)];
                }
            }
            else
            {
                // 일반방 (전투 또는 퍼즐)
                var normalCategories = new[] 
                { 
                    RoomModule.ModuleCategory.Combat, 
                    RoomModule.ModuleCategory.Puzzle,
                    RoomModule.ModuleCategory.Corridor
                };
                nextCategory = normalCategories[UnityEngine.Random.Range(0, normalCategories.Length)];
            }
            
            // 해당 카테고리의 모듈 필터링
            List<RoomModule> categoryModules = themeModules.Where(m => m.category == nextCategory).ToList();
            if (categoryModules.Count == 0)
            {
                // 해당 카테고리 모듈이 없으면 다른 모듈 사용
                categoryModules = themeModules;
            }
            
            // 랜덤 모듈 선택
            RoomModule nextModule = categoryModules[UnityEngine.Random.Range(0, categoryModules.Count)];
            
            // 호환되는 연결점 찾기
            List<int> compatiblePoints = new List<int>();
            for (int i = 0; i < nextModule.connectionPoints.Length; i++)
            {
                ConnectionPoint point = nextModule.connectionPoints[i];
                if (AreConnectionsCompatible(connectionPoint, point))
                {
                    compatiblePoints.Add(i);
                }
            }
            
            if (compatiblePoints.Count == 0)
            {
                continue; // 호환되는 연결점이 없으면 다른 연결점 시도
            }
            
            int nextConnectionIndex = compatiblePoints[UnityEngine.Random.Range(0, compatiblePoints.Count)];
            ConnectionPoint nextConnectionPoint = nextModule.connectionPoints[nextConnectionIndex];
            
            // 위치 계산 (현재 방의 연결점 위치 + 방향 오프셋 - 다음 방의 연결점 위치)
            Vector2 directionOffset = Vector2.zero;
            switch (connectionPoint.direction)
            {
                case ConnectionPoint.ConnectionDirection.Up:
                    directionOffset = new Vector2(0, 2); // 위로 2칸
                    break;
                case ConnectionPoint.ConnectionDirection.Down:
                    directionOffset = new Vector2(0, -2); // 아래로 2칸
                    break;
                case ConnectionPoint.ConnectionDirection.Right:
                    directionOffset = new Vector2(2, 0); // 오른쪽으로 2칸
                    break;
                case ConnectionPoint.ConnectionDirection.Left:
                    directionOffset = new Vector2(-2, 0); // 왼쪽으로 2칸
                    break;
            }
            
            Vector2 rotatedConnPoint = RotatePoint(connectionPoint.position, Vector2.zero, 
                currentRoom.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 currentConnPointWorld = currentRoom.position + rotatedConnPoint;
            
            Vector2 nextPosition = currentConnPointWorld + directionOffset - nextConnectionPoint.position;
            
            // 위치 충돌 검사
            bool isOverlapping = false;
            foreach (var placedModule in placedModules)
            {
                if (Vector2.Distance(placedModule.position, nextPosition) < 3f) // 3칸 이내에 다른 방이 있으면 충돌
                {
                    isOverlapping = true;
                    break;
                }
            }
            
            if (isOverlapping)
            {
                continue; // 위치 충돌시 다른 연결점 시도
            }
            
            // 새 방 생성 및 추가
            PlacedModule newRoom = new PlacedModule
            {
                moduleData = nextModule,
                position = nextPosition,
                rotationStep = 0,
                connections = new List<PlacedModule.ConnectionInfo>()
            };
            
            placedModules.Add(newRoom);
            openRooms.Add(newRoom);
            placedRoomCount++;
            
            // 두 방 연결
            ConnectModules(currentRoom, connectionIndex, newRoom, nextConnectionIndex);
        }
        
        Debug.Log($"Generated map with {placedRoomCount} rooms using {theme} theme.");
    }

    private void SaveMap()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Map",
            savePath,
            mapName + ".json",
            "json"
        );
        
        if (string.IsNullOrEmpty(path))
            return;
            
        // 상대 경로로 변환 (에셋 데이터베이스와 호환을 위해)
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }
        
        // 맵 데이터 생성
        RoomData mapData = new RoomData();
        mapData.placedModules = new List<RoomData.PlacedModuleData>();
        
        foreach (var module in placedModules)
        {
            RoomData.PlacedModuleData moduleData = new RoomData.PlacedModuleData();
            moduleData.moduleGUID = module.moduleData.assetGUID;
            moduleData.position = module.position;
            moduleData.rotationStep = module.rotationStep;
            moduleData.connections = new List<RoomData.ConnectionData>();
            
            foreach (var connection in module.connections)
            {
                // 오직 한쪽 방향의 연결만 저장 (중복 방지)
                if (connection.connectedModule == null)
                    continue;
                    
                string connectedGUID = connection.connectedModule.moduleData.assetGUID;
                if (string.Compare(moduleData.moduleGUID, connectedGUID) < 0 || 
                    (moduleData.moduleGUID == connectedGUID && connection.connectionPointIndex < connection.connectedPointIndex))
                {
                    RoomData.ConnectionData connectionData = new RoomData.ConnectionData();
                    connectionData.connectionPointIndex = connection.connectionPointIndex;
                    connectionData.connectedModuleGUID = connectedGUID;
                    connectionData.connectedPointIndex = connection.connectedPointIndex;
                    
                    moduleData.connections.Add(connectionData);
                }
            }
            
            mapData.placedModules.Add(moduleData);
        }
        
        // JSON으로 변환하여 저장
        string json = JsonUtility.ToJson(mapData, true);
        System.IO.File.WriteAllText(path, json);
        
        AssetDatabase.Refresh();
        
        // 맵 이름 업데이트
        mapName = System.IO.Path.GetFileNameWithoutExtension(path);
        
        // 저장 경로 기억
        savePath = System.IO.Path.GetDirectoryName(path);
        if (savePath.StartsWith(Application.dataPath))
        {
            savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
        }
        
        EditorUtility.DisplayDialog("Map Saved", $"Map saved successfully to {path}", "OK");
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("Load Map", savePath, "json");
        if (string.IsNullOrEmpty(path))
            return;
            
        // 상대 경로로 변환
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }
        
        savePath = Path.GetDirectoryName(path);
        
        // 파일 읽기
        string json = File.ReadAllText(path);
        
        // JSON 역직렬화
        RoomData mapData = JsonUtility.FromJson<RoomData>(json);
        
        // 기존 맵 초기화
        placedModules.Clear();
        selectedPlacedModule = null;
        
        // 임시 Dictionary (GUID -> PlacedModule)
        Dictionary<string, PlacedModule> moduleDict = new Dictionary<string, PlacedModule>();
        
        // 모듈 생성
        foreach (var moduleData in mapData.placedModules)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(moduleData.moduleGUID);
            RoomModule moduleAsset = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            
            if (moduleAsset != null)
            {
                PlacedModule module = new PlacedModule
                {
                    moduleData = moduleAsset,
                    position = moduleData.position,
                    rotationStep = moduleData.rotationStep,
                    connections = new List<PlacedModule.ConnectionInfo>()
                };
                
                placedModules.Add(module);
                moduleDict[moduleData.moduleGUID] = module;
            }
        }
        
        // 연결 정보 복원
        for (int i = 0; i < mapData.placedModules.Count; i++)
        {
            var moduleData = mapData.placedModules[i];
            
            if (i < placedModules.Count)
            {
                PlacedModule module = placedModules[i];
                
                foreach (var connData in moduleData.connections)
                {
                    if (moduleDict.TryGetValue(connData.connectedModuleGUID, out PlacedModule connectedModule))
                    {
                        PlacedModule.ConnectionInfo conn = new PlacedModule.ConnectionInfo
                        {
                            connectionPointIndex = connData.connectionPointIndex,
                            connectedModule = connectedModule,
                            connectedPointIndex = connData.connectedPointIndex
                        };
                        
                        module.connections.Add(conn);
                    }
                }
            }
        }
        
        // 맵 이름 설정
        mapName = Path.GetFileNameWithoutExtension(path);
        
        Repaint();
    }

    private void NewMap()
    {
        // 기존 모듈을 풀로 반환 (재사용)
        modulePool.AddRange(placedModules);
        placedModules.Clear();
        
        selectedPlacedModule = null;
        selectedModule = null;
        isPlacingModule = false;
        mapName = "New Map";
        
        Repaint();
    }

    private void CreateModuleAsset()
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            EditorUtility.DisplayDialog("Error", "Module name cannot be empty.", "OK");
            return;
        }

        // 저장 경로 확인 및 생성
        if (!Directory.Exists(modulePath))
        {
            Directory.CreateDirectory(modulePath);
        }

        // 스크립터블 오브젝트 생성
        RoomModule moduleAsset = ScriptableObject.CreateInstance<RoomModule>();
        moduleAsset.modulePrefab = modulePrefab;
        moduleAsset.thumbnail = thumbnail;
        moduleAsset.category = category;
        moduleAsset.theme = theme; // 환경 테마 설정 추가
        moduleAsset.isSpecialRoom = isSpecialRoom;
        moduleAsset.connectionPoints = connectionPoints.ToArray();

        // 프리팹 크기 자동 계산
        if (modulePrefab != null)
        {
            // 프리팹에서 크기 정보 가져오기
            Renderer[] renderers = modulePrefab.GetComponentsInChildren<Renderer>();
            Collider2D[] colliders = modulePrefab.GetComponentsInChildren<Collider2D>();
            
            if (renderers.Length > 0 || colliders.Length > 0)
            {
                Bounds bounds = new Bounds(modulePrefab.transform.position, Vector3.zero);
                
                // 렌더러 바운드 통합
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                
                // 콜라이더 바운드 통합
                foreach (Collider2D collider in colliders)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                
                // X, Y 크기만 저장 (2D 게임 기준)
                moduleAsset.moduleSize = new Vector2(bounds.size.x, bounds.size.y);
                
                // 최소 크기 보장
                if (moduleAsset.moduleSize.x < 0.1f) moduleAsset.moduleSize.x = 1f;
                if (moduleAsset.moduleSize.y < 0.1f) moduleAsset.moduleSize.y = 1f;
            }
        }

        // 에셋 저장
        string assetPath = modulePath + moduleName + ".asset";
        AssetDatabase.CreateAsset(moduleAsset, assetPath);
        AssetDatabase.SaveAssets();

        // 에셋 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = moduleAsset;
        
        // 모듈 목록 갱신
        LoadModules();

        Debug.Log("Module created at: " + assetPath);
    }

    // 호환 가능한 연결점 찾기
    private void FindCompatibleConnectionPoints(RoomModule module, Vector2 worldPosition)
    {
        compatibleConnectionPoints.Clear();
        nearestCompatibleModule = null;
        nearestCompatiblePointIndex = -1;
        
        float minDistance = float.MaxValue;
        
        // 배치된 모든 모듈의 모든 연결점 확인
        foreach (var placedModule in placedModules)
        {
            if (placedModule.moduleData.connectionPoints == null)
                continue;
                
            for (int i = 0; i < placedModule.moduleData.connectionPoints.Length; i++)
            {
                // 배치된 모듈의 연결점
                ConnectionPoint targetPoint = placedModule.moduleData.connectionPoints[i];
                
                // 연결 가능한 다른 회전된 위치 계산
                Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                    placedModule.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 targetWorldPos = placedModule.position + rotatedTargetPoint;
                
                // 이 모듈의 모든 연결점과 비교
                for (int j = 0; j < module.connectionPoints.Length; j++)
                {
                    ConnectionPoint sourcePoint = module.connectionPoints[j];
                    
                    // 두 연결점이 호환되는지 확인
                    if (AreConnectionsCompatible(sourcePoint, targetPoint))
                    {
                        // 연결 가능한 포인트 목록에 추가
                        if (!compatibleConnectionPoints.Contains(j))
                        {
                            compatibleConnectionPoints.Add(j);
                        }
                        
                        // 소스 포인트의 위치 계산 (가상 배치 위치 기준)
                        Vector2 sourcePointPos = sourcePoint.position;
                        Vector2 sourceWorldPos = worldPosition + sourcePointPos;
                        
                        // 두 포인트 간의 거리 계산
                        float distance = Vector2.Distance(sourceWorldPos, targetWorldPos);
                        
                        // 일정 거리 이내에 있고, 최소 거리라면 호환 포인트 설정
                        if (distance < 2.0f && distance < minDistance)
                        {
                            minDistance = distance;
                            nearestCompatibleModule = placedModule;
                            nearestCompatiblePointIndex = i;
                        }
                    }
                }
            }
        }
    }
    
    // 두 연결점 호환 여부 확인
    private bool AreConnectionsCompatible(ConnectionPoint pointA, ConnectionPoint pointB)
    {
        // 방향이 반대여야 호환됨
        bool directionsOpposite = 
            (pointA.direction == ConnectionPoint.ConnectionDirection.Up && pointB.direction == ConnectionPoint.ConnectionDirection.Down) ||
            (pointA.direction == ConnectionPoint.ConnectionDirection.Down && pointB.direction == ConnectionPoint.ConnectionDirection.Up) ||
            (pointA.direction == ConnectionPoint.ConnectionDirection.Left && pointB.direction == ConnectionPoint.ConnectionDirection.Right) ||
            (pointA.direction == ConnectionPoint.ConnectionDirection.Right && pointB.direction == ConnectionPoint.ConnectionDirection.Left);
            
        // 타입 호환성 검사
        bool typesCompatible = true;
        
        // 일방통행과 특수 문은 특별한 호환성 규칙이 있을 수 있음
        if (pointA.type == ConnectionPoint.ConnectionType.OneWay || pointB.type == ConnectionPoint.ConnectionType.OneWay)
        {
            // 일방통행은 특정 방향으로만 연결 가능할 수 있음
            // 여기서는 단순히 양쪽 다 일방통행이면 연결 불가능으로 설정
            typesCompatible = !(pointA.type == ConnectionPoint.ConnectionType.OneWay && 
                               pointB.type == ConnectionPoint.ConnectionType.OneWay);
        }
        
        return directionsOpposite && typesCompatible;
    }

    private PlacedModule GetModuleFromPool()
    {
        // 풀에서 사용 가능한 모듈 찾기
        PlacedModule module = modulePool.FirstOrDefault(m => !placedModules.Contains(m));
        
        if (module == null)
        {
            // 없으면 새로 생성
            module = new PlacedModule
            {
                connections = new List<PlacedModule.ConnectionInfo>()
            };
            modulePool.Add(module);
        }
        
        return module;
    }

    // 가시 영역 계산 메서드
    private Rect GetVisibleWorldRect(Rect mapRect)
    {
        // 화면 영역을 월드 좌표로 변환
        Vector2 topLeft = GetWorldPosition(new Vector2(mapRect.x, mapRect.y), mapRect);
        Vector2 bottomRight = GetWorldPosition(new Vector2(mapRect.x + mapRect.width, mapRect.y + mapRect.height), mapRect);
        
        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }
    
    // 모듈 맵에 그리기
    private void DrawModuleInMap(PlacedModule module, Rect mapRect)
    {
        if (module == null || module.moduleData == null) return;
        
        DrawModule(mapRect, module);
    }
    
    // 간소화된 연결 표시
    private void DrawSimplifiedConnections(List<PlacedModule> modules, Rect mapRect)
    {
        foreach (var module in modules)
        {
            Vector2 from = GetScreenPosition(module.position, mapRect);
            
            foreach (var connection in module.connections)
            {
                if (connection.connectedModule == null) continue;
                
                // 양방향 연결 중복 방지
                if (modules.IndexOf(connection.connectedModule) < modules.IndexOf(module))
                    continue;
                
                Vector2 to = GetScreenPosition(connection.connectedModule.position, mapRect);
                
                Handles.color = new Color(0.2f, 0.8f, 0.8f, 0.5f);
                Handles.DrawLine(from, to);
            }
        }
    }
    
    // 연결선 그리기
    private void DrawConnectionLine(Vector2 start, Vector2 end, ConnectionPoint.ConnectionType type, Rect mapRect)
    {
        Vector2 startScreen = GetScreenPosition(start, mapRect);
        Vector2 endScreen = GetScreenPosition(end, mapRect);
        
        Color lineColor = GetConnectionTypeColor(type);
        lineColor.a = 0.7f; // 약간 투명하게
        
        Handles.color = lineColor;
        Handles.DrawLine(startScreen, endScreen);
        
        // 연결 타입에 따른 추가 표시
        if (type == ConnectionPoint.ConnectionType.OneWay)
        {
            // 방향 표시 (화살표)
            Vector2 dir = (endScreen - startScreen).normalized;
            Vector2 mid = Vector2.Lerp(startScreen, endScreen, 0.5f);
            float arrowSize = 10f;
            
            Vector2 left = mid - new Vector2(dir.y, -dir.x) * arrowSize + dir * arrowSize;
            Vector2 right = mid + new Vector2(dir.y, -dir.x) * arrowSize + dir * arrowSize;
            
            Handles.DrawLine(mid, left);
            Handles.DrawLine(mid, right);
        }
        else if (type == ConnectionPoint.ConnectionType.LockedDoor || type == ConnectionPoint.ConnectionType.AbilityGate)
        {
            // 잠긴 문이나 능력 관문 표시 (자물쇠 아이콘)
            Vector2 mid = Vector2.Lerp(startScreen, endScreen, 0.5f);
            float iconSize = 15f;
            Rect iconRect = new Rect(mid.x - iconSize/2, mid.y - iconSize/2, iconSize, iconSize);
            
            Handles.DrawSolidDisc(mid, Vector3.forward, iconSize/2);
        }
    }
    
    // 맵 연결선 그리기
    private void DrawConnections(Rect mapRect)
    {
        DrawConnections(mapRect, placedModules);
    }

    private void InitializeGUIStyles()
    {
        // 모듈 버튼 스타일 초기화
        moduleButtonStyle = new GUIStyle(GUI.skin.button);
        moduleButtonStyle.fixedWidth = 60;
        moduleButtonStyle.fixedHeight = 60;
        moduleButtonStyle.imagePosition = ImagePosition.ImageAbove;
        moduleButtonStyle.fontSize = 9;
        moduleButtonStyle.wordWrap = true;
    }
    
    // RoomData 클래스 추가 (맵 저장/로드용)
    [System.Serializable]
    private class RoomData
    {
        public List<PlacedModuleData> placedModules = new List<PlacedModuleData>();
        
        [System.Serializable]
        public class PlacedModuleData
        {
            public string moduleGUID;
            public Vector2 position;
            public int rotationStep;
            public List<ConnectionData> connections = new List<ConnectionData>();
        }
        
        [System.Serializable]
        public class ConnectionData
        {
            public int connectionPointIndex;
            public string connectedModuleGUID;
            public int connectedPointIndex;
        }
    }

    // 배치 모드 취소 메서드
    private void CancelModulePlacement()
    {
        selectedModule = null;
        isPlacingModule = false;
        Debug.Log("모듈 배치 모드를 취소했습니다.");
    }

    // 팔로워 모듈 그리기
    private void DrawFollowerModule(RoomModule module, Vector2 worldPos, Rect mapRect)
    {
        if (module == null)
            return;
            
        // 호환 가능한 연결점이 있으면 위치 조정
        Vector2 adjustedPosition = worldPos;
        
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0 && 
            compatibleConnectionPoints.Count > 0)
        {
            int sourcePointIndex = compatibleConnectionPoints[0];
            
            // 타겟 모듈의 연결점 위치
            ConnectionPoint targetPoint = nearestCompatibleModule.moduleData.connectionPoints[nearestCompatiblePointIndex];
            Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                nearestCompatibleModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 targetWorldPos = nearestCompatibleModule.position + rotatedTargetPoint;
            
            // 소스 모듈의 연결점 위치
            ConnectionPoint sourcePoint = module.connectionPoints[sourcePointIndex];
            
            // 연결점 방향에 따라 적절히 위치 조정하여 L자 형태 유지
            Vector2 sourceOffset = sourcePoint.position;
            
            // 타겟 위치에서 소스 연결점의 오프셋을 빼서 모듈 위치 계산
            adjustedPosition = targetWorldPos - sourceOffset;
            
            // 연결선 미리보기 표시
            Handles.color = Color.green;
            Handles.DrawLine(
                GetScreenPosition(targetWorldPos, mapRect),
                GetScreenPosition(adjustedPosition + sourcePoint.position, mapRect)
            );
        }
        
        // 모듈 위치에서 스크린 위치 계산
        Vector2 screenPos = GetScreenPosition(adjustedPosition, mapRect);
        
        // 모듈 크기 적용
        Vector2 moduleSize = module.moduleSize;
        float width = moduleSize.x * 50 * zoomLevel;
        float height = moduleSize.y * 50 * zoomLevel;
        
        // 회전 단계에 따라 가로/세로 치환
        if (0 % 2 == 1) // 90도 또는 270도 회전 (현재는 미구현)
        {
            float temp = width;
            width = height;
            height = temp;
        }
        
        // 모듈 영역 계산
        Rect moduleRect = new Rect(screenPos.x - width / 2, screenPos.y - height / 2, width, height);
        
        // 반투명 박스로 표시
        Color originalColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        
        // 호환 가능한 연결점에 따라 색상 변경
        Color moduleColor;
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0)
        {
            moduleColor = new Color(0.4f, 1f, 0.4f, 0.5f); // 연결 가능할 때 녹색
        }
        else
        {
            moduleColor = GetModuleCategoryColor(module.category);
            moduleColor.a = 0.5f; // 반투명으로 설정
        }
        
        // 컬러 박스로 표시
        Handles.DrawSolidRectangleWithOutline(moduleRect, moduleColor, Color.white);
        
        // 모듈 이름 표시
        GUIStyle moduleNameStyle = new GUIStyle(GUI.skin.label);
        moduleNameStyle.alignment = TextAnchor.MiddleCenter;
        moduleNameStyle.normal.textColor = Color.white;
        moduleNameStyle.fontStyle = FontStyle.Bold;
        GUI.Label(moduleRect, module.name, moduleNameStyle);
        
        // 연결점 표시
        if (showConnectionPoints && module.connectionPoints != null)
        {
            for (int i = 0; i < module.connectionPoints.Length; i++)
            {
                Vector2 pointPos = module.connectionPoints[i].position;
                Vector2 connectionScreenPos = screenPos + pointPos * zoomLevel;
                
                // 현재 호환 가능한 연결점 강조
                if (compatibleConnectionPoints.Contains(i))
                {
                    Handles.color = Color.green;
                    Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, 8f);
                }
                
                Handles.color = GetConnectionTypeColor(module.connectionPoints[i].type);
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, 5f);
            }
        }
        
        // 색상 복원
        GUI.color = originalColor;
    }

    private void HandleMapEvents(Event e, Rect mapRect)
    {
        // 항상 화면 업데이트 요청
        Repaint();
        
        bool mouseInMapArea = mapRect.Contains(e.mousePosition);
        
        // Delete 키를 누르면 선택된 모듈 삭제
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && selectedPlacedModule != null)
        {
            // 연결된 모듈에서 이 모듈과의 연결 제거
            RemoveModuleConnections(selectedPlacedModule);
            
            // 모듈 제거
            placedModules.Remove(selectedPlacedModule);
            selectedPlacedModule = null;
            e.Use();
            Repaint();
            return;
        }
        
        // 현재 상태에 따른 이벤트 처리
        if (currentState == EditorState.Normal)
        {
            if (mouseInMapArea)
            {
                // 모듈 배치 모드일 경우
                if (selectedModule != null)
                {
                    HandleModulePlacement(e, mapRect);
                }
                // 일반 맵 조작 모드
                else
                {
                    HandleMapInteractions(e, mapRect);
                }
            }
        }
        // 연결 드래그 모드
        else if (currentState == EditorState.DraggingConnection && draggingModule != null && draggingConnectionIndex >= 0)
        {
            HandleConnectionDrag(e, mapRect);
        }

        // 마우스 위치 정보 표시 기능 추가
        if (e.type == EventType.MouseMove)
        {
            Vector2 worldPos = GetWorldPosition(e.mousePosition, mapRect);
            
            // 모듈 타일 좌표계와 유니티 타일맵 좌표계 계산
            Vector2 moduleTileCoord = new Vector2(
                Mathf.Floor(worldPos.x / TILE_WIDTH),
                Mathf.Floor(worldPos.y / TILE_HEIGHT)
            );
            
            Vector2 unityTileCoord = new Vector2(
                moduleTileCoord.x * RoomModule.UNITY_TILES_PER_MODULE_TILE,
                moduleTileCoord.y * RoomModule.UNITY_TILES_PER_MODULE_TILE
            );
            
            // 상태 바에 좌표 정보 표시
            string mouseInfo = string.Format(
                "모듈 타일 좌표: ({0}, {1}) | 유니티 타일맵 좌표: ({2}, {3})",
                moduleTileCoord.x, moduleTileCoord.y,
                unityTileCoord.x, unityTileCoord.y
            );
            
            // 에디터 하단 상태바에 정보 표시 (리페인트 빈도 제한)
            float timeSinceLastRepaint = (float)EditorApplication.timeSinceStartup - lastRepaintTime;
            if (timeSinceLastRepaint > 0.1f) // 10fps로 제한하여 성능 향상
            {
                lastRepaintTime = (float)EditorApplication.timeSinceStartup;
                EditorGUIUtility.AddCursorRect(mapRect, MouseCursor.Arrow);
                
                // 상태바 메시지 설정
                EditorUtility.DisplayProgressBar("모듈 위치 정보", mouseInfo, 0);
                EditorUtility.ClearProgressBar();
            }
        }
    }

    private void HandleModulePlacement(Event e, Rect mapRect)
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector2 worldPos = GetWorldPosition(e.mousePosition, mapRect);
        
        // 그리드에 스냅 (타일 그리드 또는 일반 그리드)
        if (useRoomTileGrid)
        {
            // 타일 크기 기준 스냅
            worldPos.x = Mathf.Round(worldPos.x / TILE_WIDTH) * TILE_WIDTH;
            worldPos.y = Mathf.Round(worldPos.y / TILE_HEIGHT) * TILE_HEIGHT;
        }
        else
        {
            // 일반 그리드 스냅
            worldPos.x = Mathf.Round(worldPos.x / GRID_SIZE) * GRID_SIZE;
            worldPos.y = Mathf.Round(worldPos.y / GRID_SIZE) * GRID_SIZE;
        }
        
        // 호환 가능한 연결점 찾기
        FindCompatibleConnectionPoints(selectedModule, worldPos);
        
        // 호환 가능한 연결점이 있는 경우 위치 조정
        Vector2 adjustedPosition = worldPos;
        
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0 && 
            compatibleConnectionPoints.Count > 0)
        {
            int sourcePointIndex = compatibleConnectionPoints[0];
            
            // 타겟 모듈의 연결점 위치
            ConnectionPoint targetPoint = nearestCompatibleModule.moduleData.connectionPoints[nearestCompatiblePointIndex];
            Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                nearestCompatibleModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 targetWorldPos = nearestCompatibleModule.position + rotatedTargetPoint;
            
            // 소스 모듈의 연결점 위치
            ConnectionPoint sourcePoint = selectedModule.connectionPoints[sourcePointIndex];
            
            // 연결점 방향에 따라 적절히 위치 조정하여 L자 형태 유지
            Vector2 sourceOffset = sourcePoint.position;
            
            // 타겟 위치에서 소스 연결점의 오프셋을 빼서 모듈 위치 계산
            adjustedPosition = targetWorldPos - sourceOffset;
            
            // 디버그 로그
            Debug.Log($"조정된 위치: {adjustedPosition}, 원본 위치: {worldPos}, " +
                     $"타겟 연결점: {targetWorldPos}, 소스 오프셋: {sourceOffset}");
        }
        
        // 마우스 클릭 이벤트 처리
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0) // 좌클릭 - 모듈 배치
            {
                // 모듈 배치
                PlacedModule newModule = new PlacedModule
                {
                    moduleData = selectedModule,
                    position = adjustedPosition, // 조정된 위치 사용
                    rotationStep = 0,
                    connections = new List<PlacedModule.ConnectionInfo>()
                };
                
                placedModules.Add(newModule);
                
                // 가장 가까운 호환 가능한 연결점이 있으면 연결 생성
                if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0 && 
                    compatibleConnectionPoints.Count > 0)
                {
                    int sourcePointIndex = compatibleConnectionPoints[0];
                    ConnectModules(newModule, sourcePointIndex, nearestCompatibleModule, nearestCompatiblePointIndex);
                }
                
                // 배치 후 계속 배치 모드 유지 또는 선택 모드로 전환 (선택 사항)
                // selectedModule = null; // 주석 해제하면 한 번 배치 후 선택 모드로 전환
                
                e.Use();
                Repaint();
            }
            else if (e.button == 1) // 우클릭 - 배치 취소
            {
                CancelModulePlacement();
                e.Use();
                Repaint();
            }
        }
        // 중간 마우스 버튼으로 모듈 회전
        else if (e.type == EventType.MouseDown && e.button == 2) // 중간 마우스 버튼
        {
            // 모듈 회전 구현 (배치 전에 프리뷰 회전)
            int rotationStep = 0; // 현재 미리보기 회전 상태를 추적하는 변수 필요
            rotationStep = (rotationStep + 1) % 4; // 90도씩 회전
            e.Use();
            Repaint();
        }
        // 키보드 Escape로 배치 모드 취소
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            CancelModulePlacement();
            e.Use();
            Repaint();
        }
        // 마우스 이동 시 화면 갱신
        else if (e.type == EventType.MouseMove)
        {
            Repaint();
        }
        // 키보드 R키로 모듈 회전
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            // 모듈 회전 구현
            if (selectedPlacedModule != null)
            {
                selectedPlacedModule.rotationStep = (selectedPlacedModule.rotationStep + 1) % 4;
                e.Use();
                Repaint();
            }
        }
    }

    private void HandleMapInteractions(Event e, Rect mapRect)
    {
        // 연결점 검사 (현재 선택된 모듈이 있을 경우)
        if (selectedPlacedModule != null && selectedPlacedModule.moduleData.connectionPoints != null)
        {
            for (int i = 0; i < selectedPlacedModule.moduleData.connectionPoints.Length; i++)
            {
                ConnectionPoint point = selectedPlacedModule.moduleData.connectionPoints[i];
                Vector2 rotatedPoint = RotatePoint(point.position, Vector2.zero, selectedPlacedModule.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 worldPointPos = selectedPlacedModule.position + rotatedPoint;
                Vector2 screenPos = GetScreenPosition(worldPointPos, mapRect);
                
                // 연결점 클릭 확인 (화면 좌표 거리 체크)
                float distance = Vector2.Distance(e.mousePosition, screenPos);
                if (distance < 10f * zoomLevel) // 연결점 클릭 허용 범위 (화면 크기에 비례)
                {
                    // 연결 드래그 상태로 전환
                    currentState = EditorState.DraggingConnection;
                    draggingConnectionIndex = i;
                    draggingModule = selectedPlacedModule; // 두 변수 모두 설정
                    draggingConnectionModule = selectedPlacedModule;
                    e.Use();
                    return;
                }
            }
        }
        
        // 일반 맵 상호작용 (모듈 선택 등)
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0) // 좌클릭
            {
                // 모듈 선택
                foreach (var module in placedModules)
                {
                    Vector2 screenPos = GetScreenPosition(module.position, mapRect);
                    // 모듈 크기 기반 선택 영역 계산
                    Vector2 moduleSize = module.moduleData.moduleSize;
                    float width = moduleSize.x * 50f * zoomLevel;
                    float height = moduleSize.y * 50f * zoomLevel;
                    Rect moduleRect = new Rect(screenPos.x - width / 2, screenPos.y - height / 2, width, height);
                    
                    if (moduleRect.Contains(e.mousePosition))
                    {
                        selectedPlacedModule = module;
                        e.Use();
                        return;
                    }
                }
                
                // 빈 공간 클릭 시 선택 해제
                selectedPlacedModule = null;
                e.Use();
            }
            else if (e.button == 1 && selectedPlacedModule != null) // 우클릭 메뉴
            {
                ShowModuleContextMenu(selectedPlacedModule);
                e.Use();
                return;
            }
        }
        // 맵 이동 (드래그)
        else if (e.type == EventType.MouseDrag && e.button == 0)
        {
            mapOffset += e.delta;
            e.Use();
            Repaint();
        }
    }

    private void ShowModuleContextMenu(PlacedModule module)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Rotate CW"), false, () => {
            module.rotationStep = (module.rotationStep + 1) % 4;
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Rotate CCW"), false, () => {
            module.rotationStep = (module.rotationStep + 3) % 4; // +3 = -1 mod 4
            Repaint();
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("Delete Module"), false, () => {
            RemoveModuleConnections(module);
            placedModules.Remove(module);
            selectedPlacedModule = null;
            Repaint();
        });
        
        menu.AddItem(new GUIContent("Deselect"), false, () => {
            selectedPlacedModule = null;
            Repaint();
        });
        
        menu.ShowAsContext();
    }

    private void HandleConnectionDrag(Event e, Rect mapRect)
    {
        // 필요한 변수가 없으면 상태 초기화
        if (draggingModule == null && draggingConnectionModule != null)
        {
            draggingModule = draggingConnectionModule;
        }
        else if (draggingModule == null)
        {
            // 잘못된 상태, 드래그 모드 종료
            currentState = EditorState.Normal;
            draggingConnectionIndex = -1;
            draggingConnectionModule = null;
            return;
        }

        // 월드 좌표 변환
        Vector2 worldPos = GetWorldPosition(e.mousePosition, mapRect);
        
        // 드래깅 중인 모듈의 연결점 위치 계산
        ConnectionPoint sourcePoint = draggingModule.moduleData.connectionPoints[draggingConnectionIndex];
        Vector2 rotatedSourcePoint = RotatePoint(sourcePoint.position, Vector2.zero, 
            draggingModule.rotationStep * 90 * Mathf.Deg2Rad);
        Vector2 sourceWorldPos = draggingModule.position + rotatedSourcePoint;
        
        // 가장 가까운 호환 가능한 연결점 찾기
        PlacedModule nearestModule = null;
        int nearestPointIndex = -1;
        float minDistance = 2.0f; // 최대 연결 거리
        
        // 모든 배치된 모듈을 검사
        foreach (var module in placedModules)
        {
            // 자기 자신은 건너뜀
            if (module == draggingModule) continue;
            
            // 각 모듈의 연결점을 검사
            for (int i = 0; i < module.moduleData.connectionPoints.Length; i++)
            {
                ConnectionPoint point = module.moduleData.connectionPoints[i];
                Vector2 rotatedPoint = RotatePoint(point.position, Vector2.zero, 
                    module.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 pointWorldPos = module.position + rotatedPoint;
                
                // 거리 계산
                float distance = Vector2.Distance(worldPos, pointWorldPos);
                if (distance < minDistance)
                {
                    // 연결점 호환성 검사
                    if (AreConnectionsCompatible(sourcePoint, point))
                    {
                        // 이미 연결되어 있는지 확인
                        bool alreadyConnected = draggingModule.connections.Any(c => 
                            c.connectionPointIndex == draggingConnectionIndex && 
                            c.connectedModule == module && 
                            c.connectedPointIndex == i);
                            
                        if (!alreadyConnected)
                        {
                            nearestModule = module;
                            nearestPointIndex = i;
                            minDistance = distance;
                        }
                    }
                }
            }
        }
        
        // 가장 가까운 호환 가능한 모듈과 포인트 설정
        nearestCompatibleModule = nearestModule;
        nearestCompatiblePointIndex = nearestPointIndex;
        
        // 드래그 라인 그리기용 위치 업데이트
        dragStartPos = sourceWorldPos;
        dragEndPos = worldPos;
        
        // 연결선 그리기 - 호환 가능한 타겟이 있으면 해당 위치로, 없으면 마우스 위치로
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0)
        {
            ConnectionPoint targetPoint = nearestCompatibleModule.moduleData.connectionPoints[nearestCompatiblePointIndex];
            Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                nearestCompatibleModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 targetWorldPos = nearestCompatibleModule.position + rotatedTargetPoint;
            
            DrawConnectionLine(sourceWorldPos, targetWorldPos, sourcePoint.type, mapRect);
        }
        else
        {
            DrawConnectionLine(sourceWorldPos, worldPos, sourcePoint.type, mapRect);
        }
        
        // 마우스 업 이벤트 처리
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            // 유효한 타겟이 있으면 연결 생성
            if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0)
            {
                ConnectModules(draggingModule, draggingConnectionIndex, nearestCompatibleModule, nearestCompatiblePointIndex);
                Debug.Log($"Connected {draggingModule.moduleData.name} point {draggingConnectionIndex} to {nearestCompatibleModule.moduleData.name} point {nearestCompatiblePointIndex}");
            }
            
            // 드래그 상태 초기화
            draggingConnectionIndex = -1;
            draggingModule = null;
            currentState = EditorState.Normal;
            nearestCompatibleModule = null;
            nearestCompatiblePointIndex = -1;
            
            e.Use();
        }
        
        // 화면 갱신
        Repaint();
    }

    private void ConnectModules(PlacedModule sourceModule, int sourcePointIndex, PlacedModule targetModule, int targetPointIndex)
    {
        // 모듈 간 연결 추가
        PlacedModule.ConnectionInfo connectionInfo = new PlacedModule.ConnectionInfo
        {
            connectionPointIndex = sourcePointIndex,
            connectedModule = targetModule,
            connectedPointIndex = targetPointIndex
        };
        
        // 이미 연결되어 있는지 확인
        bool alreadyConnected = sourceModule.connections.Any(c => 
            c.connectionPointIndex == sourcePointIndex && 
            c.connectedModule == targetModule && 
            c.connectedPointIndex == targetPointIndex);
            
        if (!alreadyConnected)
        {
            sourceModule.connections.Add(connectionInfo);
            
            // 양방향 연결 (필요시)
            PlacedModule.ConnectionInfo reverseConnection = new PlacedModule.ConnectionInfo
            {
                connectionPointIndex = targetPointIndex,
                connectedModule = sourceModule,
                connectedPointIndex = sourcePointIndex
            };
            
            bool reverseAlreadyConnected = targetModule.connections.Any(c => 
                c.connectionPointIndex == targetPointIndex && 
                c.connectedModule == sourceModule && 
                c.connectedPointIndex == sourcePointIndex);
                
            if (!reverseAlreadyConnected)
            {
                targetModule.connections.Add(reverseConnection);
            }
        }
    }

    private void RemoveModuleConnections(PlacedModule module)
    {
        // 다른 모듈과의 연결 제거
        foreach (var otherModule in placedModules)
        {
            if (otherModule != module)
            {
                otherModule.connections.RemoveAll(c => c.connectedModule == module);
            }
        }
    }

    private Vector2 GetScreenPosition(Vector2 worldPosition, Rect mapRect)
    {
        return new Vector2(
            mapRect.x + mapOffset.x + worldPosition.x * zoomLevel,
            mapRect.y + mapOffset.y + worldPosition.y * zoomLevel
        );
    }

    private Vector2 GetWorldPosition(Vector2 screenPosition, Rect mapRect)
    {
        return new Vector2(
            (screenPosition.x - mapRect.x - mapOffset.x) / zoomLevel,
            (screenPosition.y - mapRect.y - mapOffset.y) / zoomLevel
        );
    }

    private Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
    {
        // 90도 단위 회전만 사용한다면 캐싱할 수 있음
        int rotationKey = Mathf.RoundToInt(angle * Mathf.Rad2Deg) % 360;
        
        Matrix4x4 rotMatrix;
        if (!rotationMatrices.TryGetValue(rotationKey, out rotMatrix))
        {
            float s = Mathf.Sin(angle);
            float c = Mathf.Cos(angle);
            
            // 회전 행렬 생성
            rotMatrix = new Matrix4x4();
            rotMatrix[0, 0] = c;  rotMatrix[0, 1] = -s; rotMatrix[0, 2] = 0; rotMatrix[0, 3] = 0;
            rotMatrix[1, 0] = s;  rotMatrix[1, 1] = c;  rotMatrix[1, 2] = 0; rotMatrix[1, 3] = 0;
            rotMatrix[2, 0] = 0;  rotMatrix[2, 1] = 0;  rotMatrix[2, 2] = 1; rotMatrix[2, 3] = 0;
            rotMatrix[3, 0] = 0;  rotMatrix[3, 1] = 0;  rotMatrix[3, 2] = 0; rotMatrix[3, 3] = 1;
            
            rotationMatrices[rotationKey] = rotMatrix;
        }
        
        // 피벗 기준 회전
        Vector2 centered = point - pivot;
        Vector4 rotated4 = rotMatrix * new Vector4(centered.x, centered.y, 0, 1);
        Vector2 rotated = new Vector2(rotated4.x, rotated4.y) + pivot;
        
        return rotated;
    }

    [MenuItem("Metroidvania/Test/Show Grid Scale Info")]
    private static void ShowGridScaleInfo()
    {
        string info = 
            $"[모듈 에디터 그리드 스케일 정보]\n" +
            $"1 모듈 타일 = {RoomModule.UNITY_TILES_PER_MODULE_TILE} 유니티 타일맵 타일\n" +
            $"기본 룸 크기: {RoomModule.STANDARD_ROOM_WIDTH_TILES}x{RoomModule.STANDARD_ROOM_HEIGHT_TILES} 모듈 타일\n" +
            $"기본 룸 크기(유니티 타일맵): {RoomModule.STANDARD_ROOM_WIDTH_TILES * RoomModule.UNITY_TILES_PER_MODULE_TILE}x{RoomModule.STANDARD_ROOM_HEIGHT_TILES * RoomModule.UNITY_TILES_PER_MODULE_TILE} 타일\n" +
            $"\n" +
            $"유니티 씬에서 테스트 그리드를 생성하려면 메뉴에서 [Metroidvania → Test → Create Test Grid In Scene]을 선택하세요.";
            
        EditorUtility.DisplayDialog("그리드 스케일 정보", info, "확인");
    }
    
    // 에디터 메뉴 추가
    [MenuItem("Metroidvania/Grid Settings/Set 10 Unity Tiles = 1 Module Tile")]
    private static void SetTenUnityTilesToOneModuleTile()
    {
        if (EditorUtility.DisplayDialog("그리드 스케일 설정", 
            "10 유니티 타일맵 타일 = 1 모듈 타일 스케일로 설정하시겠습니까?\n" +
            "이 설정은 이미 적용되어 있습니다.", 
            "확인", "취소"))
        {
            EditorUtility.DisplayDialog("설정 완료", 
                "유니티 타일맵 10칸 = 모듈 에디터 1칸 비율이 이미 적용되어 있습니다.\n" +
                "테스트 그리드를 생성하려면 [Metroidvania → Test → Create Test Grid In Scene] 메뉴를 사용하세요.", 
                "확인");
        }
    }

    // 유니티 좌표계와 에디터 좌표계 사이의 변환 함수 추가
    private Vector3 EditorToUnityPosition(Vector2 editorPos, int rotationStep)
    {
        // 유니티 타일맵 기준으로 변환
        Vector3 unityPos = new Vector3(
            editorPos.x * RoomModule.UNITY_TILES_PER_MODULE_TILE,
            editorPos.y * RoomModule.UNITY_TILES_PER_MODULE_TILE,
            0
        );
        
        // 회전 적용
        float angle = rotationStep * 90f;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        // 반환 전 회전 정보 고려
        return unityPos;
    }

    // 에디터 전용 테스트 함수 추가 - 에디터와 유니티 씬의 좌표 일치 검증
    [MenuItem("Metroidvania/Test/Verify Editor and Scene Coordinates")]
    private static void VerifyEditorSceneCoordinates()
    {
        MetroidvaniaMapEditor editor = GetWindow<MetroidvaniaMapEditor>();
        
        // 현재 에디터에 배치된 모듈들의 좌표를 유니티 좌표계로 변환하여 표시
        string info = "에디터-유니티 좌표 변환 정보:\n\n";
        
        int count = 0;
        foreach (var module in editor.placedModules)
        {
            if (count >= 5) // 최대 5개만 표시
                break;
                
            Vector3 unityPos = editor.EditorToUnityPosition(module.position, module.rotationStep);
            
            info += $"모듈: {module.moduleData.name}\n";
            info += $"에디터 좌표: ({module.position.x}, {module.position.y})\n";
            info += $"회전: {module.rotationStep * 90}도\n";
            info += $"유니티 좌표: ({unityPos.x}, {unityPos.y}, {unityPos.z})\n\n";
            
            count++;
        }
        
        info += "에디터와 유니티 씬 사이의 좌표 일치 여부를 확인하세요.\n";
        info += "좌표 변환: 1 모듈 타일 = " + RoomModule.UNITY_TILES_PER_MODULE_TILE + " 유니티 타일맵 타일";
        
        EditorUtility.DisplayDialog("좌표 검증", info, "확인");
    }
}