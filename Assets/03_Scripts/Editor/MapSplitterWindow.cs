using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class MapSplitterWindow : EditorWindow
{
    // 설정 옵션
    private TextAsset mapJsonFile;
    private string outputFolder = "Assets/Scenes/Chunks";
    private float chunkSize = 100f;
    private bool createScenes = true;
    private bool splitJson = true;
    private bool regenerateSceneModules = true;
    private int surroundingChunkRange = 3; // 주변 청크 생성 범위
    private bool createSurroundingChunks = true; // 주변 청크 생성 옵션
    
    // 씬 템플릿 설정
    private SceneAsset sceneTemplate;
    private SceneAsset baseSceneAsset;
    private GameObject environmentPrefab;
    
    // 결과 데이터
    private Vector2Int minChunk = Vector2Int.zero;
    private Vector2Int maxChunk = Vector2Int.zero;
    private int totalModulesCount = 0;
    private int totalChunksCount = 0;
    private Dictionary<Vector2Int, List<RoomData.PlacedModuleData>> chunkMap;
    
    // 청크 생성 범위 제한
    private const int MAX_CHUNK_RANGE = 1000; // 최대 청크 범위 (±1000)
    
    // 접기/펼치기 상태
    private bool showSettings = true;
    private bool showTemplateSettings = true;
    private bool showResults = true;
    private bool showPreview = false;
    
    // UI 스크롤 값
    private Vector2 scrollPosition;

    [MenuItem("Tools/Map/Map Splitter")]
    public static void ShowWindow()
    {
        MapSplitterWindow window = GetWindow<MapSplitterWindow>("맵 분할 도구");
        window.minSize = new Vector2(400, 550);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 제목
        GUILayout.Space(10);
        EditorGUILayout.LabelField("맵 분할 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 기본 설정
        showSettings = EditorGUILayout.Foldout(showSettings, "기본 설정", true);
        if (showSettings)
        {
            EditorGUI.indentLevel++;
            mapJsonFile = EditorGUILayout.ObjectField("맵 JSON 파일", mapJsonFile, typeof(TextAsset), false) as TextAsset;
            chunkSize = EditorGUILayout.FloatField("청크 크기", chunkSize);
            outputFolder = EditorGUILayout.TextField("출력 폴더", outputFolder);

            if (GUILayout.Button("출력 폴더 생성"))
            {
                CreateOutputFolder();
            }

            splitJson = EditorGUILayout.Toggle("JSON 분할", splitJson);
            createScenes = EditorGUILayout.Toggle("씬 생성", createScenes);
            regenerateSceneModules = EditorGUILayout.Toggle("SceneModules 다시 생성", regenerateSceneModules);
            
            // 주변 청크 생성 옵션 추가
            createSurroundingChunks = EditorGUILayout.Toggle("주변 청크 생성", createSurroundingChunks);
            if (createSurroundingChunks)
            {
                EditorGUI.indentLevel++;
                surroundingChunkRange = EditorGUILayout.IntSlider("주변 청크 범위", surroundingChunkRange, 1, 5);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
        }
        
        // 템플릿 설정
        showTemplateSettings = EditorGUILayout.Foldout(showTemplateSettings, "템플릿 설정", true);
        if (showTemplateSettings)
        {
            EditorGUI.indentLevel++;
            sceneTemplate = EditorGUILayout.ObjectField("씬 템플릿", sceneTemplate, typeof(SceneAsset), false) as SceneAsset;
            baseSceneAsset = EditorGUILayout.ObjectField("기본 씬", baseSceneAsset, typeof(SceneAsset), false) as SceneAsset;
            environmentPrefab = EditorGUILayout.ObjectField("환경 프리팹", environmentPrefab, typeof(GameObject), false) as GameObject;
            EditorGUI.indentLevel--;
        }
        
        GUILayout.Space(15);
        
        // 처리 버튼
        GUI.enabled = mapJsonFile != null;
        if (GUILayout.Button("맵 분석하기", GUILayout.Height(30)))
        {
            AnalyzeMap();
        }
        
        GUI.enabled = chunkMap != null && chunkMap.Count > 0;
        if (GUILayout.Button("맵 분할 실행", GUILayout.Height(30)))
        {
            SplitMap();
        }
        GUI.enabled = true;
        
        GUILayout.Space(15);
        
        // 결과 표시
        showResults = EditorGUILayout.Foldout(showResults, "분석 결과", true);
        if (showResults && chunkMap != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("총 모듈 수:", totalModulesCount.ToString());
            EditorGUILayout.LabelField("총 청크 수:", totalChunksCount.ToString());
            EditorGUILayout.LabelField("청크 범위:", $"X({minChunk.x}~{maxChunk.x}), Y({minChunk.y}~{maxChunk.y})");
            EditorGUI.indentLevel--;
            
            showPreview = EditorGUILayout.Foldout(showPreview, "청크 미리보기", true);
            if (showPreview)
            {
                EditorGUI.indentLevel++;
                foreach (var chunk in chunkMap)
                {
                    EditorGUILayout.LabelField($"청크 ({chunk.Key.x}, {chunk.Key.y})", 
                        $"모듈 수: {chunk.Value.Count}");
                }
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void CreateOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string[] folderLevels = outputFolder.Split('/');
            string currentPath = folderLevels[0];
            
            for (int i = 1; i < folderLevels.Length; i++)
            {
                string newFolder = folderLevels[i];
                string checkPath = Path.Combine(currentPath, newFolder);
                
                if (!AssetDatabase.IsValidFolder(checkPath))
                {
                    AssetDatabase.CreateFolder(currentPath, newFolder);
                }
                
                currentPath = checkPath;
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"폴더 생성됨: {outputFolder}");
        }
        else
        {
            Debug.Log($"폴더가 이미 존재함: {outputFolder}");
        }
    }

    private void AnalyzeMap()
    {
        if (mapJsonFile == null) 
        {
            EditorUtility.DisplayDialog("오류", "맵 JSON 파일을 선택해주세요.", "확인");
            return;
        }
        
        try
        {
            // JSON 파싱
            RoomData mapData = JsonUtility.FromJson<RoomData>(mapJsonFile.text);
            if (mapData == null || mapData.placedModules == null || mapData.placedModules.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "JSON 파일 파싱 실패 또는 모듈이 없습니다.", "확인");
                return;
            }
            
            // 모듈을 청크로 그룹화
            chunkMap = new Dictionary<Vector2Int, List<RoomData.PlacedModuleData>>();
            minChunk = new Vector2Int(int.MaxValue, int.MaxValue);
            maxChunk = new Vector2Int(int.MinValue, int.MinValue);
            
            foreach (var module in mapData.placedModules)
            {
                // 모듈 위치를 기준으로 청크 ID 계산
                int chunkX = Mathf.FloorToInt(module.position.x / chunkSize);
                int chunkY = Mathf.FloorToInt(module.position.y / chunkSize);
                Vector2Int chunkId = new Vector2Int(chunkX, chunkY);
                
                // 최대/최소 청크 ID 갱신
                minChunk.x = Mathf.Min(minChunk.x, chunkX);
                minChunk.y = Mathf.Min(minChunk.y, chunkY);
                maxChunk.x = Mathf.Max(maxChunk.x, chunkX);
                maxChunk.y = Mathf.Max(maxChunk.y, chunkY);
                
                // 청크 맵에 모듈 추가
                if (!chunkMap.ContainsKey(chunkId))
                {
                    chunkMap[chunkId] = new List<RoomData.PlacedModuleData>();
                }
                
                chunkMap[chunkId].Add(module);
            }
            
            // 결과 업데이트
            totalModulesCount = mapData.placedModules.Count;
            totalChunksCount = chunkMap.Count;
            
            Debug.Log($"맵 분석 완료: {totalModulesCount}개 모듈, {totalChunksCount}개 청크");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("오류", $"맵 분석 중 오류 발생: {e.Message}", "확인");
            Debug.LogError($"맵 분석 오류: {e}");
        }
    }
    
    private void SplitMap()
    {
        if (chunkMap == null || chunkMap.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "먼저 맵을 분석해주세요.", "확인");
            return;
        }
        
        // 폴더 생성
        CreateOutputFolder();
        
        // 진행 상황 표시
        EditorUtility.DisplayProgressBar("맵 분할 중", "맵 분할 준비...", 0f);
        
        try
        {
            int processedChunks = 0;
            
            // 주변 청크를 포함한 전체 청크 목록 생성
            Dictionary<Vector2Int, List<RoomData.PlacedModuleData>> allChunks = new Dictionary<Vector2Int, List<RoomData.PlacedModuleData>>(chunkMap);
            
            // 주변 청크 생성 옵션이 활성화된 경우
            if (createScenes && createSurroundingChunks)
            {
                // 기존 청크의 ID들을 복사
                HashSet<Vector2Int> existingChunkIds = new HashSet<Vector2Int>(chunkMap.Keys);
                
                // 원본 청크 주변으로 빈 청크 추가
                foreach (var chunkId in existingChunkIds)
                {
                    // 청크 ID 유효성 검사
                    if (!IsValidChunkId(chunkId))
                    {
                        Debug.LogWarning($"유효하지 않은 청크 ID 건너뜀: {chunkId}");
                        continue;
                    }
                    
                    for (int x = -surroundingChunkRange; x <= surroundingChunkRange; x++)
                    {
                        for (int y = -surroundingChunkRange; y <= surroundingChunkRange; y++)
                        {
                            // 비정상적인 청크 ID가 생성되지 않도록 검사
                            if (chunkId.x + x < -MAX_CHUNK_RANGE || chunkId.x + x > MAX_CHUNK_RANGE ||
                                chunkId.y + y < -MAX_CHUNK_RANGE || chunkId.y + y > MAX_CHUNK_RANGE)
                            {
                                continue;
                            }
                            
                            Vector2Int newChunkId = new Vector2Int(chunkId.x + x, chunkId.y + y);
                            
                            // 청크 ID 유효성 재검사
                            if (!IsValidChunkId(newChunkId))
                            {
                                continue;
                            }
                            
                            // 아직 존재하지 않는 청크인 경우 빈 모듈 리스트로 추가
                            if (!allChunks.ContainsKey(newChunkId))
                            {
                                allChunks[newChunkId] = new List<RoomData.PlacedModuleData>();
                                
                                // 전체 범위 업데이트
                                minChunk.x = Mathf.Min(minChunk.x, newChunkId.x);
                                minChunk.y = Mathf.Min(minChunk.y, newChunkId.y);
                                maxChunk.x = Mathf.Max(maxChunk.x, newChunkId.x);
                                maxChunk.y = Mathf.Max(maxChunk.y, newChunkId.y);
                            }
                        }
                    }
                }
                
                Debug.Log($"주변 청크 추가됨: 원본 {chunkMap.Count}개 + 주변 {allChunks.Count - chunkMap.Count}개 = 총 {allChunks.Count}개 청크");
            }
            
            // 청크별 JSON 생성
            if (splitJson)
            {
                string jsonFolder = Path.Combine(outputFolder, "Json");
                if (!AssetDatabase.IsValidFolder(jsonFolder))
                {
                    string[] parts = jsonFolder.Split('/');
                    AssetDatabase.CreateFolder(Path.Combine(parts.Take(parts.Length - 1).ToArray()), parts.Last());
                }
                
                // JSON은 모듈이 있는 원본 청크만 생성
                foreach (var chunk in chunkMap)
                {
                    float progress = (float)processedChunks / totalChunksCount;
                    EditorUtility.DisplayProgressBar("맵 분할 중", $"청크 JSON 생성 중: {chunk.Key}", progress);
                    
                    // 청크별 JSON 생성
                    CreateChunkJson(chunk.Key, chunk.Value, jsonFolder);
                    processedChunks++;
                }
            }
            
            // 청크별 씬 생성
            if (createScenes)
            {
                processedChunks = 0;
                string scenesFolder = Path.Combine(outputFolder, "Scenes");
                if (!AssetDatabase.IsValidFolder(scenesFolder))
                {
                    string[] parts = scenesFolder.Split('/');
                    AssetDatabase.CreateFolder(Path.Combine(parts.Take(parts.Length - 1).ToArray()), parts.Last());
                }
                
                // 모든 청크(원본 + 주변)에 대해 씬 생성
                int totalAllChunks = allChunks.Count;
                foreach (var chunk in allChunks)
                {
                    float progress = (float)processedChunks / totalAllChunks;
                    EditorUtility.DisplayProgressBar("맵 분할 중", $"청크 씬 생성 중: {chunk.Key}", progress);
                    
                    // 청크별 씬 생성
                    CreateChunkScene(chunk.Key, chunk.Value, scenesFolder);
                    processedChunks++;
                }
            }
            
            // SceneModuleData 생성
            if (regenerateSceneModules)
            {
                EditorUtility.DisplayProgressBar("맵 분할 중", "SceneModuleData 생성 중...", 0.9f);
                CreateSceneModuleData(allChunks);
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayProgressBar("맵 분할 중", "완료", 1.0f);
            EditorUtility.DisplayDialog("완료", $"{allChunks.Count}개 청크 생성 완료!", "확인");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("오류", $"맵 분할 중 오류 발생: {e.Message}", "확인");
            Debug.LogError($"맵 분할 오류: {e}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    private void CreateChunkJson(Vector2Int chunkId, List<RoomData.PlacedModuleData> modules, string folder)
    {
        // 청크 데이터 생성
        RoomData chunkData = new RoomData();
        chunkData.placedModules = new List<RoomData.PlacedModuleData>(modules);
        
        // JSON 생성
        string jsonContent = JsonUtility.ToJson(chunkData, true);
        string filePath = Path.Combine(folder, $"Chunk_{chunkId.x}_{chunkId.y}.json");
        
        // 파일 저장
        File.WriteAllText(filePath, jsonContent);
        AssetDatabase.ImportAsset(filePath);
        
        Debug.Log($"청크 JSON 생성됨: {filePath}");
    }
    
    private void CreateChunkScene(Vector2Int chunkId, List<RoomData.PlacedModuleData> modules, string folder)
    {
        string sceneName = $"Chunk_{chunkId.x}_{chunkId.y}";
        string scenePath = Path.Combine(folder, $"{sceneName}.unity");
        
        // 씬 생성 방식 결정
        if (sceneTemplate != null)
        {
            // 템플릿 씬 복제
            string templatePath = AssetDatabase.GetAssetPath(sceneTemplate);
            AssetDatabase.CopyAsset(templatePath, scenePath);
        }
        else
        {
            // 빈 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        
        // 씬 열기
        var createdScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // 환경 추가
        if (environmentPrefab != null)
        {
            GameObject env = PrefabUtility.InstantiatePrefab(environmentPrefab) as GameObject;
            if (env != null)
            {
                env.name = $"Environment_{chunkId.x}_{chunkId.y}";
                
                // 청크 중심 위치 계산
                float centerX = (chunkId.x * chunkSize) + (chunkSize / 2);
                float centerY = (chunkId.y * chunkSize) + (chunkSize / 2);
                env.transform.position = new Vector3(centerX, centerY, 0);
            }
        }
        
        // 청크 정보 객체 추가
        GameObject chunkInfoObj = new GameObject($"ChunkInfo_{chunkId.x}_{chunkId.y}");
        ChunkInfo chunkInfo = chunkInfoObj.AddComponent<ChunkInfo>();
        chunkInfo.chunkId = chunkId;
        chunkInfo.chunkSize = chunkSize;
        chunkInfo.moduleCount = modules.Count;
        chunkInfo.boundMin = new Vector2(chunkId.x * chunkSize, chunkId.y * chunkSize);
        chunkInfo.boundMax = new Vector2((chunkId.x + 1) * chunkSize, (chunkId.y + 1) * chunkSize);
        
        // 씬 저장 후 닫기
        EditorSceneManager.SaveScene(createdScene);
        
        Debug.Log($"청크 씬 생성됨: {scenePath}");
    }
    
    private void CreateSceneModuleData(Dictionary<Vector2Int, List<RoomData.PlacedModuleData>> allChunks)
    {
        // SceneModuleData는 MapManager의 일부이므로 먼저 찾아야 함
        MapManager mapManager = null;
        
        // 기본 씬에서 MapManager 검색
        if (baseSceneAsset != null)
        {
            // 현재 씬 저장
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            
            // 기본 씬 열기
            string baseScenePath = AssetDatabase.GetAssetPath(baseSceneAsset);
            var baseScene = EditorSceneManager.OpenScene(baseScenePath, OpenSceneMode.Single);
            
            // MapManager 찾기
            mapManager = GameObject.FindObjectOfType<MapManager>();
        }
        
        if (mapManager == null)
        {
            Debug.LogWarning("MapManager를 찾을 수 없습니다. SceneModuleData를 생성할 수 없습니다.");
            return;
        }
        
        // 새 SceneModuleData 생성
        mapManager.sceneModules.Clear();
        
        // 청크별로 GUID 매핑
        Dictionary<string, List<string>> chunkGuids = new Dictionary<string, List<string>>();
        
        // 각 청크에 포함된 모듈 GUID 수집
        foreach (var chunk in allChunks)
        {
            string chunkKey = $"Chunk_{chunk.Key.x}_{chunk.Key.y}";
            List<string> guids = new List<string>();
            
            foreach (var module in chunk.Value)
            {
                if (!guids.Contains(module.moduleGUID))
                {
                    guids.Add(module.moduleGUID);
                }
            }
            
            chunkGuids[chunkKey] = guids;
        }
        
        // SceneModuleData 생성
        foreach (var entry in chunkGuids)
        {
            var sceneData = new MapManager.SceneModuleData
            {
                sceneName = entry.Key,
                moduleGuids = entry.Value
            };
            
            mapManager.sceneModules.Add(sceneData);
        }
        
        // 씬 저장
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"SceneModuleData 생성 완료: {mapManager.sceneModules.Count}개 씬");
    }

    /// <summary>
    /// 청크 ID가 유효한지 검사
    /// </summary>
    private bool IsValidChunkId(Vector2Int chunkId)
    {
        // int.MinValue나 int.MaxValue에 가까운 비정상적인 값 체크
        if (chunkId.x < -MAX_CHUNK_RANGE || chunkId.x > MAX_CHUNK_RANGE || 
            chunkId.y < -MAX_CHUNK_RANGE || chunkId.y > MAX_CHUNK_RANGE)
        {
            return false;
        }
        
        // 오버플로우 방지를 위한 추가 검사
        if (chunkId.x == int.MinValue || chunkId.x == int.MaxValue || 
            chunkId.y == int.MinValue || chunkId.y == int.MaxValue)
        {
            return false;
        }
        
        return true;
    }
} 