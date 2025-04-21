using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class MetroidvaniaMapEditor : EditorWindow
{
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
    private PlacedModule draggingConnectionModule;

    private string mapName = "New Map";
    private string savePath = "Assets/MetroidvaniaMapData/";
    
    // 모듈 생성에 필요한 변수들
    private string moduleName = "";
    private GameObject modulePrefab;
    private Texture2D thumbnail;
    private RoomModule.ModuleCategory category;
    private bool isSpecialRoom;
    private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

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
        GetWindow<MetroidvaniaMapEditor>("Metroidvania Map Editor");
    }

    private void OnEnable()
    {
        // 모든 RoomModule 에셋 로드
        LoadModules();

        // GUI 스타일 초기화
        moduleButtonStyle = new GUIStyle(GUI.skin.button);
        moduleButtonStyle.fixedWidth = 100;
        moduleButtonStyle.fixedHeight = 100;
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
        EditorGUILayout.BeginHorizontal();

        // 좌측 패널 - 모듈 선택
        DrawModuleSelectionPanel();

        // 우측 패널 - 맵 편집
        DrawMapEditingPanel();

        EditorGUILayout.EndHorizontal();

        // 하단 패널 - 저장/불러오기 기능
        DrawBottomPanel();
    }

    private void DrawModuleSelectionPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(150));

        EditorGUILayout.LabelField("Available Modules", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 카테고리별로 모듈 그룹화
        var groupedModules = availableModules.GroupBy(m => m.category);

        foreach (var group in groupedModules)
        {
            EditorGUILayout.LabelField(group.Key.ToString(), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            int count = 0;

            foreach (var module in group)
            {
                // 썸네일 버튼 표시
                GUIContent content = new GUIContent(module.thumbnail, module.name);
                if (GUILayout.Button(content, moduleButtonStyle))
                {
                    selectedModule = module;
                    isPlacingModule = true;
                }

                count++;
                if (count % 3 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        
        // 모듈 생성 섹션
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Module", EditorStyles.boldLabel);
        
        moduleName = EditorGUILayout.TextField("Module Name:", moduleName);
        modulePrefab = (GameObject)EditorGUILayout.ObjectField("Module Prefab:", modulePrefab, typeof(GameObject), false);
        thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail:", thumbnail, typeof(Texture2D), false);
        category = (RoomModule.ModuleCategory)EditorGUILayout.EnumPopup("Category:", category);
        isSpecialRoom = EditorGUILayout.Toggle("Is Special Room:", isSpecialRoom);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connection Points", EditorStyles.boldLabel);

        // 기존 연결점 표시
        for (int i = 0; i < connectionPoints.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Connection Point " + (i + 1));
            connectionPoints[i].position = EditorGUILayout.Vector2Field("Position:", connectionPoints[i].position);
            connectionPoints[i].direction = (ConnectionPoint.ConnectionDirection)EditorGUILayout.EnumPopup("Direction:", connectionPoints[i].direction);
            connectionPoints[i].type = (ConnectionPoint.ConnectionType)EditorGUILayout.EnumPopup("Type:", connectionPoints[i].type);

            if (GUILayout.Button("Remove Point"))
            {
                connectionPoints.RemoveAt(i);
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndVertical();
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
        // 그리드 배경 그리기
        DrawGrid(mapRect);

        // 각 모듈 그리기
        foreach (var module in placedModules)
        {
            DrawModule(mapRect, module);
        }

        // 모듈 간 연결선 그리기
        DrawConnections(mapRect);

        // 선택된 모듈의 프리뷰 그리기
        if (isPlacingModule && selectedModule != null)
        {
            Vector2 mousePos = Event.current.mousePosition;
            if (mapRect.Contains(mousePos))
            {
                DrawModulePreview(mapRect, selectedModule, GetWorldPosition(mousePos, mapRect));
            }
        }

        // 연결 드래그 라인 그리기
        if (draggingConnectionIndex >= 0 && draggingConnectionModule != null)
        {
            ConnectionPoint point = draggingConnectionModule.moduleData.connectionPoints[draggingConnectionIndex];
            Vector2 rotatedPoint = RotatePoint(point.position, Vector2.zero, draggingConnectionModule.rotationStep * 90 * Mathf.Deg2Rad);
            Vector2 worldPointPos = draggingConnectionModule.position + rotatedPoint;
            Vector2 startScreenPos = GetScreenPosition(worldPointPos, mapRect);
            
            Vector2 endScreenPos = Event.current.mousePosition;
            
            Handles.color = Color.yellow;
            Handles.DrawLine(startScreenPos, endScreenPos);
        }

        // 이벤트 처리
        HandleMapEvents(mapRect);
    }

    private void DrawGrid(Rect mapRect)
    {
        // 여기에 그리드 그리기 코드 구현
        GUI.Box(mapRect, "", EditorStyles.helpBox);
        
        // 그리드 크기 설정
        float gridSize = 100 * zoomLevel;
        
        // 그리드 오프셋 계산
        float offsetX = mapOffset.x % gridSize;
        float offsetY = mapOffset.y % gridSize;
        
        // 가로 그리드 라인
        for (float y = offsetY; y <= mapRect.height; y += gridSize)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Handles.DrawLine(new Vector3(mapRect.x, mapRect.y + y), new Vector3(mapRect.x + mapRect.width, mapRect.y + y));
        }
        
        // 세로 그리드 라인
        for (float x = offsetX; x <= mapRect.width; x += gridSize)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Handles.DrawLine(new Vector3(mapRect.x + x, mapRect.y), new Vector3(mapRect.x + x, mapRect.y + mapRect.height));
        }
    }

    private void DrawModule(Rect mapRect, PlacedModule module)
    {
        Vector2 screenPos = GetScreenPosition(module.position, mapRect);
        
        // 모듈 위치가 화면 범위를 벗어나면 그리지 않음
        if (screenPos.x < -100 || screenPos.x > mapRect.width + 100 || 
            screenPos.y < -100 || screenPos.y > mapRect.height + 100)
            return;
        
        float size = 100 * zoomLevel;
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
        
        // 연결점 그리기
        if (module.moduleData.connectionPoints != null)
        {
            for (int i = 0; i < module.moduleData.connectionPoints.Length; i++)
            {
                ConnectionPoint point = module.moduleData.connectionPoints[i];
                Vector2 pointPos = RotatePoint(point.position, Vector2.zero, module.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 connectionScreenPos = screenPos + pointPos * zoomLevel;
                
                Rect connectionRect = new Rect(connectionScreenPos.x - 5, connectionScreenPos.y - 5, 10, 10);
                
                // 연결점 표시
                Handles.color = GetConnectionTypeColor(point.type);
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, 5);
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

    private void DrawConnections(Rect mapRect)
    {
        foreach (var module in placedModules)
        {
            foreach (var connection in module.connections)
            {
                if (connection.connectedModule != null)
                {
                    // 소스 모듈의 연결점 위치 계산
                    ConnectionPoint sourcePoint = module.moduleData.connectionPoints[connection.connectionPointIndex];
                    Vector2 rotatedSourcePoint = RotatePoint(sourcePoint.position, Vector2.zero, module.rotationStep * 90 * Mathf.Deg2Rad);
                    Vector2 sourceWorldPos = module.position + rotatedSourcePoint;
                    Vector2 sourceScreenPos = GetScreenPosition(sourceWorldPos, mapRect);
                    
                    // 타겟 모듈의 연결점 위치 계산
                    ConnectionPoint targetPoint = connection.connectedModule.moduleData.connectionPoints[connection.connectedPointIndex];
                    Vector2 rotatedTargetPoint = RotatePoint(targetPoint.position, Vector2.zero, connection.connectedModule.rotationStep * 90 * Mathf.Deg2Rad);
                    Vector2 targetWorldPos = connection.connectedModule.position + rotatedTargetPoint;
                    Vector2 targetScreenPos = GetScreenPosition(targetWorldPos, mapRect);
                    
                    // 연결선 그리기
                    Handles.color = Color.cyan;
                    Handles.DrawLine(sourceScreenPos, targetScreenPos);
                }
            }
        }
    }

    private void DrawModulePreview(Rect mapRect, RoomModule module, Vector2 worldPosition)
    {
        Vector2 screenPos = GetScreenPosition(worldPosition, mapRect);
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
                
                Rect connectionRect = new Rect(connectionScreenPos.x - 5, connectionScreenPos.y - 5, 10, 10);
                
                // 연결점 표시
                Handles.color = GetConnectionTypeColor(point.type);
                Handles.DrawSolidDisc(connectionScreenPos, Vector3.forward, 5);
            }
        }
        
        // 색상 복원
        GUI.color = originalColor;
    }

    private void HandleMapEvents(Rect mapRect)
    {
        Event e = Event.current;
        
        // 맵 영역 내부에서만 처리
        if (!mapRect.Contains(e.mousePosition))
            return;
            
        Vector2 worldPos = GetWorldPosition(e.mousePosition, mapRect);
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // 좌클릭
                {
                    if (isPlacingModule && selectedModule != null)
                    {
                        // 새 모듈 배치
                        PlaceModule(selectedModule, worldPos);
                        e.Use();
                    }
                    else
                    {
                        // 기존 모듈 선택 또는 연결점 선택
                        HandleModuleSelection(worldPos, mapRect);
                        e.Use();
                    }
                }
                else if (e.button == 1) // 우클릭
                {
                    // 모듈 배치 취소
                    if (isPlacingModule)
                    {
                        isPlacingModule = false;
                        e.Use();
                    }
                    // 연결 취소
                    else if (draggingConnectionIndex >= 0)
                    {
                        draggingConnectionIndex = -1;
                        draggingConnectionModule = null;
                        e.Use();
                    }
                }
                break;
                
            case EventType.MouseDrag:
                if (e.button == 2 || (e.button == 0 && e.alt)) // 중간 버튼 또는 Alt+좌클릭
                {
                    // 맵 이동
                    mapOffset += e.delta;
                    Repaint();
                    e.Use();
                }
                break;
                
            case EventType.ScrollWheel:
                // 줌 인/아웃
                float zoomDelta = -e.delta.y * 0.01f;
                float prevZoom = zoomLevel;
                zoomLevel = Mathf.Clamp(zoomLevel + zoomDelta, 0.1f, 3.0f);
                
                // 마우스 위치를 기준으로 줌 조정
                if (prevZoom != zoomLevel)
                {
                    Vector2 mousePos = e.mousePosition - new Vector2(mapRect.x, mapRect.y);
                    Vector2 mousePosWorld = (mousePos - mapOffset) / prevZoom;
                    Vector2 newOffset = mousePos - mousePosWorld * zoomLevel;
                    mapOffset = newOffset;
                    
                    Repaint();
                    e.Use();
                }
                break;
                
            case EventType.MouseMove:
                // 마우스 이동 시 리페인트 (프리뷰 업데이트)
                if (isPlacingModule && selectedModule != null)
                {
                    Repaint();
                }
                // 연결 드래그 중이면 리페인트
                else if (draggingConnectionIndex >= 0)
                {
                    Repaint();
                }
                break;
        }
    }

    private void PlaceModule(RoomModule module, Vector2 worldPosition)
    {
        // 그리드에 맞춤
        float gridSize = 1.0f;
        Vector2 snappedPosition = new Vector2(
            Mathf.Round(worldPosition.x / gridSize) * gridSize,
            Mathf.Round(worldPosition.y / gridSize) * gridSize
        );
        
        // 새 모듈 생성
        PlacedModule newModule = new PlacedModule
        {
            moduleData = module,
            position = snappedPosition,
            rotationStep = 0,
            connections = new List<PlacedModule.ConnectionInfo>()
        };
        
        placedModules.Add(newModule);
        selectedPlacedModule = newModule;
        
        // 가까운 연결점 자동 연결 (구현 시 추가)
        
        // 모듈 계속 배치 모드 유지
        // isPlacingModule = false; // 한 번만 배치하려면 이 줄 주석 해제
    }

    private void HandleModuleSelection(Vector2 worldPos, Rect mapRect)
    {
        // 연결점 검사 (현재 선택된 모듈이 있을 경우)
        if (selectedPlacedModule != null && selectedPlacedModule.moduleData.connectionPoints != null)
        {
            for (int i = 0; i < selectedPlacedModule.moduleData.connectionPoints.Length; i++)
            {
                ConnectionPoint point = selectedPlacedModule.moduleData.connectionPoints[i];
                Vector2 rotatedPoint = RotatePoint(point.position, Vector2.zero, selectedPlacedModule.rotationStep * 90 * Mathf.Deg2Rad);
                Vector2 worldPointPos = selectedPlacedModule.position + rotatedPoint;
                
                // 연결점 클릭 확인 (거리 체크)
                float distance = Vector2.Distance(worldPos, worldPointPos);
                if (distance < 0.5f) // 연결점 클릭 허용 범위
                {
                    // 연결 드래그 시작
                    draggingConnectionIndex = i;
                    draggingConnectionModule = selectedPlacedModule;
                    return;
                }
            }
        }
        
        // 연결 드래그 중 다른 모듈의 연결점 클릭 확인
        if (draggingConnectionIndex >= 0 && draggingConnectionModule != null)
        {
            foreach (var module in placedModules)
            {
                if (module == draggingConnectionModule)
                    continue;
                    
                for (int i = 0; i < module.moduleData.connectionPoints.Length; i++)
                {
                    ConnectionPoint point = module.moduleData.connectionPoints[i];
                    Vector2 rotatedPoint = RotatePoint(point.position, Vector2.zero, module.rotationStep * 90 * Mathf.Deg2Rad);
                    Vector2 worldPointPos = module.position + rotatedPoint;
                    
                    float distance = Vector2.Distance(worldPos, worldPointPos);
                    if (distance < 0.5f) // 연결점 클릭 허용 범위
                    {
                        // 모듈 연결
                        ConnectModules(draggingConnectionModule, draggingConnectionIndex, module, i);
                        
                        // 연결 드래그 종료
                        draggingConnectionIndex = -1;
                        draggingConnectionModule = null;
                        return;
                    }
                }
            }
            
            // 빈 공간 클릭 시 연결 취소
            draggingConnectionIndex = -1;
            draggingConnectionModule = null;
            return;
        }
        
        // 모듈 선택
        foreach (var module in placedModules)
        {
            // 모듈 중심점과의 거리 체크
            float distance = Vector2.Distance(worldPos, module.position);
            if (distance < 1.0f) // 모듈 선택 허용 범위
            {
                selectedPlacedModule = module;
                return;
            }
        }
        
        // 빈 공간 클릭 시 선택 해제
        selectedPlacedModule = null;
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
        float s = Mathf.Sin(angle);
        float c = Mathf.Cos(angle);
        
        // 피벗 기준 회전
        Vector2 rotated = new Vector2(
            c * (point.x - pivot.x) - s * (point.y - pivot.y) + pivot.x,
            s * (point.x - pivot.x) + c * (point.y - pivot.y) + pivot.y
        );
        
        return rotated;
    }

    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 맵 이름 입력
        EditorGUILayout.LabelField("Map Name:", GUILayout.Width(70));
        mapName = EditorGUILayout.TextField(mapName, GUILayout.Width(150));
        
        GUILayout.FlexibleSpace();
        
        // 맵 저장 및 불러오기 버튼
        if (GUILayout.Button("Save Map", EditorStyles.toolbarButton))
        {
            SaveMap();
        }
        
        if (GUILayout.Button("Load Map", EditorStyles.toolbarButton))
        {
            LoadMap();
        }
        
        if (GUILayout.Button("New Map", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("New Map", "Are you sure you want to clear the current map?", "Yes", "Cancel"))
            {
                NewMap();
            }
        }
        
        EditorGUILayout.EndHorizontal();
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
        string path = "Assets/MetroidvaniaModules/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // 스크립터블 오브젝트 생성
        RoomModule moduleAsset = ScriptableObject.CreateInstance<RoomModule>();
        moduleAsset.modulePrefab = modulePrefab;
        moduleAsset.thumbnail = thumbnail;
        moduleAsset.category = category;
        moduleAsset.isSpecialRoom = isSpecialRoom;
        moduleAsset.connectionPoints = connectionPoints.ToArray();

        // 에셋 저장
        string assetPath = path + moduleName + ".asset";
        AssetDatabase.CreateAsset(moduleAsset, assetPath);
        AssetDatabase.SaveAssets();

        // 에셋 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = moduleAsset;
        
        // 모듈 목록 갱신
        LoadModules();

        Debug.Log("Module created at: " + assetPath);
    }
}