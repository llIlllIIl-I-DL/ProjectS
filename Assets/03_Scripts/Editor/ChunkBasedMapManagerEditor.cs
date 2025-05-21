using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ChunkBasedMapManager))]
public class ChunkBasedMapManagerEditor : Editor
{
    // 탭 상태
    private enum Tab { Settings, Debugging, Help }
    private Tab currentTab = Tab.Settings;
    
    // 디버깅 옵션
    private Vector2 debugScrollPosition;
    private bool showChunkInfo = true;
    private bool showPlayerInfo = true;
    private bool showTeleportTool = true;
    private Vector2 teleportPosition = Vector2.zero;
    
    // 설정 폴드아웃 상태
    private bool showBasicSettings = true;
    private bool showChunkSettings = true;
    private bool showPrefabSettings = true;
    private bool showJsonSettings = true;
    private bool showModuleSettings = true;
    
    // 직렬화된 프로퍼티
    private SerializedProperty chunkPrefabsProperty;

    private void OnEnable()
    {
        // 직렬화된 프로퍼티 초기화
        chunkPrefabsProperty = serializedObject.FindProperty("chunkPrefabs");
    }
    
    public override void OnInspectorGUI()
    {
        ChunkBasedMapManager mapManager = (ChunkBasedMapManager)target;
        
        // 탭 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.Settings, "설정", EditorStyles.toolbarButton))
            currentTab = Tab.Settings;
        if (GUILayout.Toggle(currentTab == Tab.Debugging, "디버깅", EditorStyles.toolbarButton))
            currentTab = Tab.Debugging;
        if (GUILayout.Toggle(currentTab == Tab.Help, "도움말", EditorStyles.toolbarButton))
            currentTab = Tab.Help;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 탭 내용
        switch (currentTab)
        {
            case Tab.Settings:
                DrawSettingsTab(mapManager);
                break;
            case Tab.Debugging:
                DrawDebuggingTab(mapManager);
                break;
            case Tab.Help:
                DrawHelpTab();
                break;
        }
        
        // 변경된 내용 적용
        if (GUI.changed)
        {
            EditorUtility.SetDirty(mapManager);
        }
    }
    
    private void DrawSettingsTab(ChunkBasedMapManager mapManager)
    {
        // 기본 설정
        showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "기본 설정", true);
        if (showBasicSettings)
        {
            EditorGUI.indentLevel++;
            mapManager.playerTransform = (Transform)EditorGUILayout.ObjectField("플레이어", mapManager.playerTransform, typeof(Transform), true);
            mapManager.resourcesJsonFolder = EditorGUILayout.TextField("JSON 폴더 (Resources)", mapManager.resourcesJsonFolder);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 청크 관리 설정
        showChunkSettings = EditorGUILayout.Foldout(showChunkSettings, "청크 관리 설정", true);
        if (showChunkSettings)
        {
            EditorGUI.indentLevel++;
            mapManager.chunkSize = EditorGUILayout.FloatField("청크 크기", mapManager.chunkSize);
            mapManager.loadDistance = EditorGUILayout.FloatField("로드 거리", mapManager.loadDistance);
            mapManager.unloadDistance = EditorGUILayout.FloatField("언로드 거리", mapManager.unloadDistance);
            
            // 유효성 검사
            if (mapManager.chunkSize <= 0)
                EditorGUILayout.HelpBox("청크 크기는 0보다 커야 합니다.", MessageType.Error);
            if (mapManager.loadDistance <= 0)
                EditorGUILayout.HelpBox("로드 거리는 0보다 커야 합니다.", MessageType.Error);
            if (mapManager.unloadDistance <= mapManager.loadDistance)
                EditorGUILayout.HelpBox("언로드 거리는 로드 거리보다 커야 합니다.", MessageType.Warning);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 프리팹 기반 청크 설정
        showPrefabSettings = EditorGUILayout.Foldout(showPrefabSettings, "프리팹 청크 설정", true);
        if (showPrefabSettings)
        {
            EditorGUI.indentLevel++;
            
            // 기본 청크 프리팹 이름
            mapManager.defaultChunkPrefabName = EditorGUILayout.TextField("기본 프리팹 이름", mapManager.defaultChunkPrefabName);
            
            // 기본 청크 프리팹 직접 할당
            mapManager.defaultChunkPrefab = (GameObject)EditorGUILayout.ObjectField("기본 청크 프리팹", mapManager.defaultChunkPrefab, typeof(GameObject), false);
            
            // 프리팹 배열 필드
            EditorGUILayout.LabelField("청크 프리팹 배열");
            EditorGUI.indentLevel++;
            
            // 배열 크기 조정 필드
            EditorGUILayout.PropertyField(chunkPrefabsProperty, true);
            serializedObject.ApplyModifiedProperties();
            
            EditorGUI.indentLevel--;
            
            // 청크 루트 오브젝트
            mapManager.chunksRoot = (Transform)EditorGUILayout.ObjectField("청크 부모 Transform", mapManager.chunksRoot, typeof(Transform), true);
            
            // 프리팹 청크 관련 도움말 
            EditorGUILayout.HelpBox("프리팹 기반 로드 방식은 메모리 관리에 효율적입니다. '기본 청크 프리팹'을 직접 할당하면 Resources 폴더에서 찾을 필요 없이 사용합니다.", MessageType.Info);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // JSON 설정 수정
        showJsonSettings = EditorGUILayout.Foldout(showJsonSettings, "JSON 파일 설정", true);
        if (showJsonSettings)
        {
            EditorGUI.indentLevel++;
            mapManager.loadModulesFromJson = EditorGUILayout.Toggle("JSON 배치 데이터 사용", mapManager.loadModulesFromJson);
            using (new EditorGUI.DisabledScope(!mapManager.loadModulesFromJson))
            {
                mapManager.jsonFileFormat = EditorGUILayout.TextField("JSON 파일 형식", mapManager.jsonFileFormat);
                EditorGUILayout.HelpBox("JSON 파일은 모듈의 위치와 회전 정보를 포함합니다.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // 모듈 설정 추가
        showModuleSettings = EditorGUILayout.Foldout(showModuleSettings, "모듈 설정", true);
        if (showModuleSettings)
        {
            EditorGUI.indentLevel++;
            
            mapManager.moduleResourcesPath = EditorGUILayout.TextField("모듈 Resources 경로", mapManager.moduleResourcesPath);
            mapManager.enableDirectMapping = EditorGUILayout.Toggle("직접 매핑 활성화", mapManager.enableDirectMapping);
            
            // 모듈 매핑 표시
            if (mapManager.moduleGuidMappings != null && mapManager.moduleGuidMappings.Count > 0)
            {
                EditorGUILayout.LabelField($"모듈 매핑 수: {mapManager.moduleGuidMappings.Count}");
                EditorGUILayout.HelpBox("모듈 매핑은 GUID와 RoomModule SO를 연결합니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("모듈 매핑이 없습니다. Resources 폴더에서 RoomModule SO를 로드합니다.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }
    }
    
    private void DrawDebuggingTab(ChunkBasedMapManager mapManager)
    {
        debugScrollPosition = EditorGUILayout.BeginScrollView(debugScrollPosition);
        
        // 런타임 전용 디버깅 도구는 플레이 모드에서만 활성화
        if (Application.isPlaying)
        {
            // 플레이어 정보
            showPlayerInfo = EditorGUILayout.Foldout(showPlayerInfo, "플레이어 정보", true);
            if (showPlayerInfo && mapManager.playerTransform != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Vector3Field("위치", mapManager.playerTransform.position);
                
                // 현재 청크 계산 및 표시
                Vector3 playerPos = mapManager.playerTransform.position;
                int chunkX = Mathf.FloorToInt(playerPos.x / mapManager.chunkSize);
                int chunkY = Mathf.FloorToInt(playerPos.y / mapManager.chunkSize);
                EditorGUILayout.LabelField("현재 청크", $"({chunkX}, {chunkY})");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 청크 정보
            showChunkInfo = EditorGUILayout.Foldout(showChunkInfo, "로드된 청크", true);
            if (showChunkInfo)
            {
                EditorGUI.indentLevel++;
                List<Vector2Int> loadedChunks = mapManager.GetLoadedChunks();
                EditorGUILayout.LabelField("청크 수", loadedChunks.Count.ToString());
                
                foreach (var chunk in loadedChunks)
                {
                    EditorGUILayout.LabelField($"청크 ({chunk.x}, {chunk.y})");
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 프리팹 청크 정보
            bool showPrefabChunkInfo = EditorGUILayout.Foldout(true, "프리팹 청크 인스턴스", true);
            if (showPrefabChunkInfo)
            {
                EditorGUI.indentLevel++;
                
                // GetChunkInstances 메서드가 있으면 사용
                var instances = mapManager.GetChunkInstances();
                EditorGUILayout.LabelField("인스턴스 수", instances.Count.ToString());
                
                // 활성화된 청크 vs 비활성화된 청크 표시
                int activeCount = 0;
                foreach (var chunk in instances)
                {
                    if (chunk.Value != null && chunk.Value.activeSelf)
                        activeCount++;
                }
                
                EditorGUILayout.LabelField("활성화된 청크", activeCount.ToString());
                EditorGUILayout.LabelField("비활성화된 청크", (instances.Count - activeCount).ToString());
                
                // 청크별 상세 정보
                if (instances.Count > 0)
                {
                    EditorGUILayout.LabelField("청크 목록:", EditorStyles.boldLabel);
                    foreach (var chunk in instances)
                    {
                        if (chunk.Value != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // 청크 활성화 상태에 따라 색상 변경
                            GUI.color = chunk.Value.activeSelf ? Color.green : Color.gray;
                            EditorGUILayout.LabelField($"  청크 ({chunk.Key.x}, {chunk.Key.y})", 
                                $"[{(chunk.Value.activeSelf ? "활성" : "비활성")}] {chunk.Value.name}");
                            
                            // 색상 복원
                            GUI.color = Color.white;
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 텔레포트 도구
            showTeleportTool = EditorGUILayout.Foldout(showTeleportTool, "텔레포트 도구", true);
            if (showTeleportTool)
            {
                EditorGUI.indentLevel++;
                teleportPosition = EditorGUILayout.Vector2Field("텔레포트 위치", teleportPosition);
                
                if (GUILayout.Button("텔레포트"))
                {
                    mapManager.TeleportPlayer(teleportPosition);
                }
                
                // 청크 격자에 빠르게 텔레포트할 수 있는 버튼
                EditorGUILayout.LabelField("빠른 텔레포트:");
                EditorGUILayout.BeginHorizontal();
                for (int x = -2; x <= 2; x++)
                {
                    EditorGUILayout.BeginVertical();
                    for (int y = 2; y >= -2; y--)
                    {
                        if (GUILayout.Button($"({x}, {y})"))
                        {
                            Vector2 newPos = new Vector2(
                                (x * mapManager.chunkSize) + (mapManager.chunkSize / 2),
                                (y * mapManager.chunkSize) + (mapManager.chunkSize / 2)
                            );
                            mapManager.TeleportPlayer(newPos);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("디버깅 도구는 플레이 모드에서만 사용할 수 있습니다.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawHelpTab()
    {
        EditorGUILayout.LabelField("청크 기반 맵 매니저 사용법", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("1. 맵 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 맵 JSON 파일을 청크 단위로 분할해야 합니다.\n" +
            "- 맵 분할 도구(Tools > Map > Map Splitter)를 사용하세요.\n" +
            "- 분할된 JSON 파일은 Resources 폴더에 저장해야 합니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("2. 청크 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 청크 크기: 각 청크의 크기(단위: 유니티 단위)\n" +
            "- 로드 거리: 이 거리 내의 청크를 로드합니다.\n" +
            "- 언로드 거리: 이 거리 밖의 청크를 언로드합니다.\n" +
            "- 언로드 거리는 로드 거리보다 커야 합니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("3. 모듈 로드 방식", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- JSON 배치 데이터: 모듈의 위치와 회전 정보를 JSON 파일에서 읽어옵니다.\n" +
            "- 모듈 프리팹: RoomModule SO에서 참조됩니다.\n" +
            "- Resources 폴더에 JSON 파일과 RoomModule SO가 모두 있어야 합니다.\n" +
            "- GUID를 통해 JSON의 모듈 데이터와 SO의 프리팹이 연결됩니다.", 
            MessageType.Info);
            
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("4. 프리팹 청크 시스템", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 청크 프리팹 로드 방식으로 동작합니다.\n" +
            "- 장점: 메모리 관리가 효율적이며, 화면 깜빡임이 줄어듭니다.\n" +
            "- 기본 청크 프리팹: Inspector에서 직접 할당하거나 Resources 폴더에서 로드할 수 있습니다.\n" +
            "- 여러 종류의 청크 프리팹: 다양한 청크 형태를 배열에 추가하여 사용할 수 있습니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("맵 분할 도구 열기"))
        {
            EditorUtility.DisplayDialog("알림", "맵 분할 도구를 열려면 Tools > Map > Map Splitter 메뉴를 사용하세요.", "확인");
        }
    }
} 