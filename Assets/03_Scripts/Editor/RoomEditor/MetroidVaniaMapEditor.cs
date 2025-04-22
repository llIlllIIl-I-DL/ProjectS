using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditorInternal;

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
    private const float GRID_SIZE = 5f; // 그리드 크기

    private string mapName = "New Map";
    private string savePath = "Assets/03_Scripts/RoomData/";
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

    private void DrawModuleSelectionPanel(Rect topPanel)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(150));

        EditorGUILayout.LabelField("Available Modules", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 카테고리별로 모듈 그룹화
        var groupedModules = availableModules.GroupBy(m => m.category);

        foreach (var group in groupedModules)
        {
            EditorGUILayout.LabelField(group.Key.ToString(), EditorStyles.boldLabel);

            // 각 그룹마다 수평 레이아웃 처리 개선
            int modulesInRow = 0;
            bool isHorizontalOpen = false;
            
            for (int i = 0; i < group.Count(); i++)
            {
                var module = group.ElementAt(i);
                
                // 새 행 시작
                if (modulesInRow == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    isHorizontalOpen = true;
                }
                
                // 썸네일 버튼 표시
                GUIContent content;
                if (module.thumbnail != null)
                {
                    content = new GUIContent(module.thumbnail, module.name);
                }
                else
                {
                    // 썸네일이 없을 경우 텍스트만 표시
                    content = new GUIContent(module.name);
                }
                
                if (GUILayout.Button(content, moduleButtonStyle))
                {
                    selectedModule = module;
                    isPlacingModule = true;
                }

                modulesInRow++;
                
                // 행 완성 (3개) 또는 마지막 모듈
                if (modulesInRow >= 3 || i == group.Count() - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    isHorizontalOpen = false;
                    modulesInRow = 0;
                }
            }
            
            // 혹시 열린 수평 레이아웃이 있다면 닫기
            if (isHorizontalOpen)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        
        // 모듈 생성 섹션
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Module", EditorStyles.boldLabel);
        
        moduleName = EditorGUILayout.TextField("Module Name:", moduleName);
        modulePrefab = (GameObject)EditorGUILayout.ObjectField("Module Prefab:", modulePrefab, typeof(GameObject), false);
        thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail:", thumbnail, typeof(Texture2D), false);
        category = (RoomModule.ModuleCategory)EditorGUILayout.EnumPopup("Category:", category);
        theme = (RoomModule.EnvironmentTheme)EditorGUILayout.EnumPopup("Theme:", theme); // 환경 테마 필드 추가
        isSpecialRoom = EditorGUILayout.Toggle("Is Special Room:", isSpecialRoom);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connection Points", EditorStyles.boldLabel);

        // 기존 연결점 표시 (배열 복제로 수정 도중 변경사항 보호)
        List<ConnectionPoint> connectionPointsCopy = new List<ConnectionPoint>(connectionPoints);
        int connectionPointToDelete = -1;
        
        for (int i = 0; i < connectionPointsCopy.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Connection Point " + (i + 1));
            connectionPointsCopy[i].position = EditorGUILayout.Vector2Field("Position:", connectionPointsCopy[i].position);
            connectionPointsCopy[i].direction = (ConnectionPoint.ConnectionDirection)EditorGUILayout.EnumPopup("Direction:", connectionPointsCopy[i].direction);
            connectionPointsCopy[i].type = (ConnectionPoint.ConnectionType)EditorGUILayout.EnumPopup("Type:", connectionPointsCopy[i].type);

                if (GUILayout.Button("Remove Point"))
                {
                connectionPointToDelete = i;
                }

                EditorGUILayout.EndVertical();
            }
        
        // 루프 밖에서 삭제 처리
        if (connectionPointToDelete >= 0)
        {
            connectionPointsCopy.RemoveAt(connectionPointToDelete);
            connectionPoints = connectionPointsCopy;
        }
        else
        {
            // 변경사항 적용
            connectionPoints = connectionPointsCopy;
        }

            // 새 연결점 추가 버튼
            if (GUILayout.Button("Add Connection Point"))
            {
                connectionPoints.Add(new ConnectionPoint
                {
                    position = Vector2.zero,
                    direction = ConnectionPoint.ConnectionDirection.Right,
                    type = ConnectionPoint.ConnectionType.Normal
                });
            }

            EditorGUILayout.Space();

            // 모듈 생성 버튼
            if (GUILayout.Button("Create Module"))
            {
                CreateModuleAsset();
            }
        
        EditorGUILayout.EndVertical();
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
        
        // 그리드 크기 설정 - 줌 레벨에 따라 그리드 간격 조정
        float gridSize = 100 * zoomLevel;
        if (zoomLevel < 0.5f)
            gridSize = 200 * zoomLevel; // 줌 아웃 시 그리드 간격 넓게
        
        // 그리드 오프셋 계산
        float offsetX = mapOffset.x % gridSize;
        float offsetY = mapOffset.y % gridSize;
        
        // 그리드 투명도 설정 - 줌 레벨에 따라 조정
        float gridAlpha = 0.3f;
        if (zoomLevel < 0.5f)
            gridAlpha = 0.15f;
        else if (zoomLevel > 1.5f)
            gridAlpha = 0.4f;
        
        Color gridColor = new Color(0.5f, 0.5f, 0.5f, gridAlpha);
        
        // 가로 그리드 라인
        int lineCount = Mathf.CeilToInt(mapRect.height / gridSize) + 1;
        for (int i = 0; i < lineCount; i++)
        {
            float y = offsetY + i * gridSize;
            if (y >= 0 && y <= mapRect.height)
            {
                Handles.color = gridColor;
                Handles.DrawLine(
                    new Vector3(mapRect.x, mapRect.y + y), 
                    new Vector3(mapRect.x + mapRect.width, mapRect.y + y)
                );
            }
        }
        
        // 세로 그리드 라인
        lineCount = Mathf.CeilToInt(mapRect.width / gridSize) + 1;
        for (int i = 0; i < lineCount; i++)
        {
            float x = offsetX + i * gridSize;
            if (x >= 0 && x <= mapRect.width)
            {
                Handles.color = gridColor;
                Handles.DrawLine(
                    new Vector3(mapRect.x + x, mapRect.y), 
                    new Vector3(mapRect.x + x, mapRect.y + mapRect.height)
                );
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
        
        float size;
        
        // 초극한 단순화 모드
        if (ultraSimpleMode)
        {
            // 가장 단순한 형태로 표시 - 텍스트만 표시
            size = 30f * zoomLevel; // 작은 사이즈로
            Rect simpleRect = new Rect(screenPos.x - size, screenPos.y - size, size * 2, size * 2);
            
            // 단순 컬러 블록으로 표시
            Color fillColor = Color.gray;
            Color borderColor = (module == selectedPlacedModule) ? Color.yellow : Color.white;
            
            Handles.DrawSolidRectangleWithOutline(simpleRect, fillColor, borderColor);
            
            // 최소한의 텍스트 정보만 표시
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            
            GUI.Label(simpleRect, module.moduleData.name, labelStyle);
            
            return;
        }
        
        // 간소화된 모드 및 응급 모드
        if (emergencyMode || (zoomLevel < 0.5f && renderQuality < 1))
        {
            // 심플 모드 - 썸네일 없이 간단한 사각형만 그리기
            size = 50f * zoomLevel;
            Rect simpleRect = new Rect(screenPos.x - size, screenPos.y - size, size * 2, size * 2);
            
            Color fillColor = module.moduleData.category switch
            {
                RoomModule.ModuleCategory.Combat => new Color(1f, 0.6f, 0.6f, 0.5f),
                RoomModule.ModuleCategory.Puzzle => new Color(0.6f, 0.8f, 1f, 0.5f),
                RoomModule.ModuleCategory.Boss => new Color(1f, 0.2f, 0.2f, 0.5f),
                RoomModule.ModuleCategory.Village => new Color(0.6f, 1f, 0.6f, 0.5f),
                _ => new Color(0.8f, 0.8f, 0.8f, 0.5f)
            };
            
            Color borderColor = (module == selectedPlacedModule) ? Color.yellow : Color.white;
            
            Handles.DrawSolidRectangleWithOutline(
                new Rect(simpleRect.x, simpleRect.y, simpleRect.width, simpleRect.height),
                fillColor, borderColor
            );
            
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
        
        size = 100 * zoomLevel;
        Rect moduleRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);
        
        // 모듈 썸네일 그리기
        GUI.color = (module == selectedPlacedModule) ? Color.yellow : Color.white;
        Matrix4x4 matrixBackup = GUI.matrix;
        
        // 모듈 회전 처리
        if (module.rotationStep > 0)
        {
            GUIUtility.RotateAroundPivot(module.rotationStep * 90, screenPos);
        }
        
        // 썸네일 그리기
        if (module.moduleData.thumbnail != null)
        {
            GUI.DrawTexture(moduleRect, module.moduleData.thumbnail);
        }
        else
        {
            GUI.Box(moduleRect, module.moduleData.name);
        }
        
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

    private void DrawModulePreview(Rect mapRect)
    {
        if (selectedModule == null) return;
        
        // 마우스 위치를 월드 좌표로 변환
        Vector2 worldPos = GetWorldPosition(Event.current.mousePosition, mapRect);
        // 그리드 스냅
        worldPos.x = Mathf.Round(worldPos.x / GRID_SIZE) * GRID_SIZE;
        worldPos.y = Mathf.Round(worldPos.y / GRID_SIZE) * GRID_SIZE;
        
        Vector2 screenPos = GetScreenPosition(worldPos, mapRect);
        float size = 100 * zoomLevel;
        Rect moduleRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);
        
        // 프리뷰 반투명 설정
        Color originalColor = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.6f);
        
        // 썸네일 그리기
        if (selectedModule.thumbnail != null)
        {
            GUI.DrawTexture(moduleRect, selectedModule.thumbnail);
        }
        else
        {
            GUI.Box(moduleRect, selectedModule.name);
        }
        
        // 연결점 그리기
        if (selectedModule.connectionPoints != null)
        {
            for (int i = 0; i < selectedModule.connectionPoints.Length; i++)
            {
                ConnectionPoint point = selectedModule.connectionPoints[i];
                Vector2 connectionScreenPos = screenPos + point.position * zoomLevel;
                
                // 호환되는 연결점 강조
                bool isCompatible = compatibleConnectionPoints.Contains(i);
                
                // 연결점 표시 - 호환되는 경우 더 크고 밝게 표시
                Handles.color = isCompatible ? 
                    new Color(1f, 1f, 0.5f, 0.8f) : // 호환되는 연결점은 밝은 노란색
                    GetConnectionTypeColor(point.type);
                    
                float radius = isCompatible ? 8f : 5f;
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, radius);
            }
        }
        
        // 호환 가능한 다른 모듈의 연결점 표시
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0)
        {
            ConnectionPoint targetPoint = nearestCompatibleModule.moduleData.connectionPoints[nearestCompatiblePointIndex];
            Vector2 rotatedPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                nearestCompatibleModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 targetWorldPos = nearestCompatibleModule.position + rotatedPoint;
            Vector2 targetScreenPos = GetScreenPosition(targetWorldPos, mapRect);
            
            // 호환 가능한 타겟 포인트 강조
            Handles.color = new Color(0f, 1f, 1f, 0.8f); // 밝은 청록색
            Handles.DrawSolidDisc(targetScreenPos, Vector3.forward, 8f);
            
            // 연결선 그리기
            int compatibleSourceIndex = compatibleConnectionPoints.Count > 0 ? compatibleConnectionPoints[0] : -1;
            if (compatibleSourceIndex >= 0)
            {
                ConnectionPoint sourcePoint = selectedModule.connectionPoints[compatibleSourceIndex];
                Vector2 sourceScreenPos = screenPos + sourcePoint.position * zoomLevel;
                
                Handles.color = new Color(0f, 1f, 0.5f, 0.5f);
                Handles.DrawDottedLine(sourceScreenPos, targetScreenPos, 3f);
            }
        }
        
        // 색상 복원
        GUI.color = originalColor;
    }
    
    // 팔로워 모듈 그리기
    private void DrawFollowerModule(RoomModule module, Vector2 worldPos, Rect mapRect)
    {
        if (module == null) return;
        
        // 호환 가능한 연결점 찾기
        FindCompatibleConnectionPoints(module, worldPos);
        
        Vector2 screenPos = GetScreenPosition(worldPos, mapRect);
        float size = 100 * zoomLevel;
        Rect moduleRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);
        
        // 프리뷰 반투명 설정
        Color originalColor = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.6f);
        
        // 썸네일 그리기
        if (module.thumbnail != null)
        {
            GUI.DrawTexture(moduleRect, module.thumbnail);
        }
        else
        {
            GUI.Box(moduleRect, module.name);
        }
        
        // 연결점 그리기
        if (module.connectionPoints != null)
        {
            for (int i = 0; i < module.connectionPoints.Length; i++)
            {
                ConnectionPoint point = module.connectionPoints[i];
                Vector2 connectionScreenPos = screenPos + point.position * zoomLevel;
                
                // 호환되는 연결점 강조
                bool isCompatible = compatibleConnectionPoints.Contains(i);
                
                Handles.color = isCompatible ? Color.yellow : GetConnectionTypeColor(point.type);
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, isCompatible ? 7f : 5f);
            }
        }
        
        // 호환 가능한 다른 모듈의 연결점 표시
        if (nearestCompatibleModule != null && nearestCompatiblePointIndex >= 0)
        {
            ConnectionPoint targetPoint = nearestCompatibleModule.moduleData.connectionPoints[nearestCompatiblePointIndex];
            Vector2 rotatedPoint = RotatePoint(targetPoint.position, Vector2.zero, 
                nearestCompatibleModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 targetWorldPos = nearestCompatibleModule.position + rotatedPoint;
            Vector2 targetScreenPos = GetScreenPosition(targetWorldPos, mapRect);
            
            // 호환 가능한 타겟 포인트 강조
            Handles.color = new Color(0f, 1f, 1f, 0.8f); // 밝은 청록색
            Handles.DrawSolidDisc(targetScreenPos, Vector3.forward, 8f);
            
            // 연결선 그리기
            int compatibleSourceIndex = compatibleConnectionPoints.Count > 0 ? compatibleConnectionPoints[0] : -1;
            if (compatibleSourceIndex >= 0)
            {
                ConnectionPoint sourcePoint = module.connectionPoints[compatibleSourceIndex];
                Vector2 sourceScreenPos = screenPos + sourcePoint.position * zoomLevel;
                
                Handles.color = new Color(0f, 1f, 0.5f, 0.5f);
                Handles.DrawDottedLine(sourceScreenPos, targetScreenPos, 3f);
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
    }

    private void HandleModulePlacement(Event e, Rect mapRect)
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector2 worldPos = GetWorldPosition(e.mousePosition, mapRect);
        
        // 그리드에 스냅
        worldPos.x = Mathf.Round(worldPos.x / GRID_SIZE) * GRID_SIZE;
        worldPos.y = Mathf.Round(worldPos.y / GRID_SIZE) * GRID_SIZE;
        
        // 호환 가능한 연결점 찾기
        FindCompatibleConnectionPoints(selectedModule, worldPos);
        
        // 마우스 클릭 이벤트 처리
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0) // 좌클릭 - 모듈 배치
            {
                // 모듈 배치
                PlacedModule newModule = new PlacedModule
                {
                    moduleData = selectedModule,
                    position = worldPos,
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
        // 우클릭으로 모듈 회전
        else if (e.type == EventType.MouseDown && e.button == 2) // 중간 마우스 버튼
        {
            // 모듈 회전 구현 (배치 전에 프리뷰 회전)
            // 여기에 회전 로직 구현
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
                    float size = 50f * zoomLevel;
                    Rect moduleRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);
                    
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
        
        menu.AddSeparator("");
        
        // 그리드 크기 설정 옵션
        menu.AddItem(new GUIContent("Use Grid Snapping"), true, () => {
            // 항상 그리드 스냅을 사용 - 비활성화 불가능
            Repaint();
        });
        
        menu.ShowAsContext();
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
        int targetRoomCount = Random.Range(minRooms, maxRooms + 1);
        
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
            startModule = themeModules[Random.Range(0, themeModules.Count)];
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
            int openRoomIndex = Random.Range(0, openRooms.Count);
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
            int connectionIndex = availableConnections[Random.Range(0, availableConnections.Count)];
            ConnectionPoint connectionPoint = currentRoom.moduleData.connectionPoints[connectionIndex];
            
            // 특수방 결정 (약 20% 확률)
            bool shouldBeSpecial = Random.value < 0.2f;
            
            // 다음 방 종류 결정 (일반 방, 보스방, 특수방 등)
            RoomModule.ModuleCategory nextCategory;
            if (shouldBeSpecial)
            {
                // 보스방은 맵 끝에만 (현재 방이 1개만 연결된 경우)
                if (placedRoomCount > targetRoomCount * 0.7f && Random.value < 0.3f)
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
                    nextCategory = specialCategories[Random.Range(0, specialCategories.Length)];
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
                nextCategory = normalCategories[Random.Range(0, normalCategories.Length)];
            }
            
            // 해당 카테고리의 모듈 필터링
            List<RoomModule> categoryModules = themeModules.Where(m => m.category == nextCategory).ToList();
            if (categoryModules.Count == 0)
            {
                // 해당 카테고리 모듈이 없으면 다른 모듈 사용
                categoryModules = themeModules;
            }
            
            // 랜덤 모듈 선택
            RoomModule nextModule = categoryModules[Random.Range(0, categoryModules.Count)];
            
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
            
            int nextConnectionIndex = compatiblePoints[Random.Range(0, compatiblePoints.Count)];
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
        string path = EditorUtility.SaveFilePanel("Save Map", savePath, mapName, "json");
        if (string.IsNullOrEmpty(path))
            return;
            
        // 상대 경로로 변환
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }
        
        savePath = Path.GetDirectoryName(path);
        
        // 맵 데이터 생성
        RoomData mapData = new RoomData();
        foreach (var module in placedModules)
        {
            RoomData.PlacedModuleData moduleData = new RoomData.PlacedModuleData
            {
                moduleGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(module.moduleData)),
                position = module.position,
                rotationStep = module.rotationStep
            };
            
            // 연결 정보 저장
            foreach (var conn in module.connections)
            {
                RoomData.ConnectionData connData = new RoomData.ConnectionData
                {
                    connectionPointIndex = conn.connectionPointIndex,
                    connectedModuleGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(conn.connectedModule.moduleData)),
                    connectedPointIndex = conn.connectedPointIndex
                };
                
                moduleData.connections.Add(connData);
            }
            
            mapData.placedModules.Add(moduleData);
        }
        
        // JSON 직렬화
        string json = JsonUtility.ToJson(mapData, true);
        
        // 파일 저장
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        
        Debug.Log("Map saved to: " + path);
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
        float maxCheckDistance = 10f; // 검사 범위 제한
        float maxCheckDistanceSqr = maxCheckDistance * maxCheckDistance;
        
        // 검사 대상 모듈 필터링 - 근처 모듈만 검사 (LINQ 제거)
        List<PlacedModule> nearbyModules = new List<PlacedModule>();
        for (int i = 0; i < placedModules.Count; i++)
        {
            PlacedModule m = placedModules[i];
            if (m == null || m.moduleData == null) continue;
            
            float distSqr = (m.position - worldPosition).sqrMagnitude;
            if (distSqr < maxCheckDistanceSqr)
                nearbyModules.Add(m);
                
            // 응급 모드에서는 최대 10개 모듈만 검사
            if (emergencyMode && nearbyModules.Count >= 10)
                break;
        }
        
        for (int moduleIndex = 0; moduleIndex < nearbyModules.Count; moduleIndex++)
        {
            PlacedModule placedModule = nearbyModules[moduleIndex];
            
            // 이미 배치된 모듈의 각 연결점 검사
            for (int placedPointIndex = 0; placedPointIndex < placedModule.moduleData.connectionPoints.Length; placedPointIndex++)
            {
                ConnectionPoint placedPoint = placedModule.moduleData.connectionPoints[placedPointIndex];
                
                // 이미 연결된 포인트는 건너뛰기 (LINQ 제거)
                bool isAlreadyConnected = false;
                for (int i = 0; i < placedModule.connections.Count; i++)
                {
                    if (placedModule.connections[i].connectionPointIndex == placedPointIndex)
                    {
                        isAlreadyConnected = true;
                        break;
                    }
                }
                
                if (isAlreadyConnected) continue;
                
                // 배치된 모듈의 연결점 월드 위치 계산
                Vector2 rotatedPlacedPoint = RotatePoint(placedPoint.position, Vector2.zero, 
                    placedModule.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 placedPointWorldPos = placedModule.position + rotatedPlacedPoint;
                
                // 프리뷰 모듈의 각 연결점 검사
                for (int modulePointIndex = 0; modulePointIndex < module.connectionPoints.Length; modulePointIndex++)
                {
                    ConnectionPoint modulePoint = module.connectionPoints[modulePointIndex];
                    
                    // 연결 호환성 검사 (방향 반대, 타입 호환 등)
                    if (AreConnectionsCompatible(modulePoint, placedPoint))
                    {
                        // 연결점 간 거리 계산
                        Vector2 modulePointWorldPos = worldPosition + modulePoint.position;
                        float distance = Vector2.Distance(modulePointWorldPos, placedPointWorldPos);
                        
                        // 가까운 호환 가능 포인트 저장
                        if (distance < 5f && distance < minDistance) // 5 유닛 내에 있고 가장 가까운 것
                        {
                            minDistance = distance;
                            compatibleConnectionPoints.Clear(); // 가장 가까운 것만 저장하도록 변경
                            compatibleConnectionPoints.Add(modulePointIndex);
                            nearestCompatibleModule = placedModule;
                            nearestCompatiblePointIndex = placedPointIndex;
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

    // 가시 영역 계산 메서드 추가
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
}