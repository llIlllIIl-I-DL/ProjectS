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
    private bool showSceneSettings = true;
    private bool showJsonSettings = true;
    
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
        
        // 씬 설정
        showSceneSettings = EditorGUILayout.Foldout(showSceneSettings, "씬 설정", true);
        if (showSceneSettings)
        {
            EditorGUI.indentLevel++;
            mapManager.sceneNameFormat = EditorGUILayout.TextField("씬 이름 형식", mapManager.sceneNameFormat);
            mapManager.baseSceneName = EditorGUILayout.TextField("기본 씬 이름", mapManager.baseSceneName);
            
            // 씬 정보 표시
            if (!string.IsNullOrEmpty(mapManager.baseSceneName))
            {
                bool baseSceneExists = false;
                
                // Build Settings에서 씬 검색
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (scene.path.Contains(mapManager.baseSceneName))
                    {
                        baseSceneExists = true;
                        break;
                    }
                }
                
                if (!baseSceneExists)
                {
                    EditorGUILayout.HelpBox("기본 씬이 Build Settings에 없습니다!", MessageType.Warning);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // JSON 설정
        showJsonSettings = EditorGUILayout.Foldout(showJsonSettings, "JSON 파일 설정", true);
        if (showJsonSettings)
        {
            EditorGUI.indentLevel++;
            mapManager.loadModulesFromJson = EditorGUILayout.Toggle("JSON에서 모듈 로드", mapManager.loadModulesFromJson);
            using (new EditorGUI.DisabledScope(!mapManager.loadModulesFromJson))
            {
                mapManager.jsonFileFormat = EditorGUILayout.TextField("JSON 파일 형식", mapManager.jsonFileFormat);
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
            
            if (GUILayout.Button("씬 빌드 설정 확인"))
            {
                CheckScenesInBuildSettings();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void CheckScenesInBuildSettings()
    {
        // 필요한 씬 목록 (예시)
        List<string> neededScenes = new List<string>();
        
        // 기본 씬
        ChunkBasedMapManager mapManager = (ChunkBasedMapManager)target;
        if (!string.IsNullOrEmpty(mapManager.baseSceneName))
        {
            neededScenes.Add(mapManager.baseSceneName);
        }
        
        // 청크 씬 (모든 청크는 미리 알 수 없으므로 경고만 표시)
        bool hasChunkScenes = false;
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path.Contains("Chunk_"))
            {
                hasChunkScenes = true;
                break;
            }
        }
        
        if (!hasChunkScenes)
        {
            EditorUtility.DisplayDialog("빌드 설정 경고", "빌드 설정에 청크 씬이 포함되어 있지 않습니다!\n모든 청크 씬을 빌드 설정에 추가해야 합니다.", "확인");
        }
        
        // 누락된 씬 검사
        List<string> missingScenes = new List<string>();
        foreach (var sceneName in neededScenes)
        {
            bool found = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path.Contains(sceneName))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                missingScenes.Add(sceneName);
            }
        }
        
        if (missingScenes.Count > 0)
        {
            string message = "다음 씬이 빌드 설정에 포함되어 있지 않습니다:\n\n";
            foreach (var scene in missingScenes)
            {
                message += $"- {scene}\n";
            }
            
            EditorUtility.DisplayDialog("빌드 설정 경고", message, "확인");
        }
        else if (neededScenes.Count > 0)
        {
            EditorUtility.DisplayDialog("빌드 설정 확인", "모든 필요한 씬이 빌드 설정에 포함되어 있습니다.", "확인");
        }
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
        
        EditorGUILayout.LabelField("2. 씬 구성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 각 청크는 별도의 씬으로 저장해야 합니다.\n" +
            "- 청크 씬의 이름은 'Chunk_X_Y' 형식으로 지정하세요.\n" +
            "- 모든 청크 씬을 빌드 설정에 추가해야 합니다.\n" +
            "- 기본 씬에는 플레이어와 이 매니저가 포함되어야 합니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("3. 청크 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 청크 크기: 각 청크의 크기(단위: 유니티 단위)\n" +
            "- 로드 거리: 이 거리 내의 청크를 로드합니다.\n" +
            "- 언로드 거리: 이 거리 밖의 청크를 언로드합니다.\n" +
            "- 언로드 거리는 로드 거리보다 커야 합니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("4. 모듈 로드 방식", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- JSON에서 모듈 로드: 활성화하면 JSON 파일에서 모듈 데이터를 읽어 인스턴스화합니다.\n" +
            "- 비활성화하면 청크 씬에 미리 배치된 모듈을 사용합니다.\n" +
            "- 런타임 환경에서도 Resources 폴더에서 JSON 파일을 찾을 수 있어야 합니다.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("맵 분할 도구 열기"))
        {
            MapSplitterWindow.ShowWindow();
        }
    }
} 