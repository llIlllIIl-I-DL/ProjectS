using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 모듈의 GUID를 찾아서 복사하는 에디터 도구
/// </summary>
public class ModuleGuidFinder : EditorWindow
{
    [MenuItem("Tools/Modules/GUID Finder")]
    private static void ShowWindow()
    {
        var window = GetWindow<ModuleGuidFinder>();
        window.titleContent = new GUIContent("모듈 GUID 찾기");
        window.Show();
    }

    private string searchFolder = "Assets";
    private string searchText = "";
    private List<RoomModule> foundModules = new List<RoomModule>();
    private List<string> moduleGuids = new List<string>();
    private Vector2 scrollPos;
    private bool showAll = false;
    private ChunkBasedMapManager mapManager;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("모듈 GUID 찾기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "이 도구를 사용하면 프로젝트 내의 모든 RoomModule 에셋의 GUID를 찾을 수 있습니다.\n" +
            "찾은 GUID는 ChunkBasedMapManager의 모듈 매핑에 사용할 수 있습니다.",
            MessageType.Info);
        
        EditorGUILayout.Space();

        // 설정
        EditorGUILayout.BeginHorizontal();
        searchFolder = EditorGUILayout.TextField("검색 폴더", searchFolder);
        if (GUILayout.Button("찾기", GUILayout.Width(100)))
        {
            FindAllModules();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        // MapManager 참조
        EditorGUILayout.BeginHorizontal();
        mapManager = (ChunkBasedMapManager)EditorGUILayout.ObjectField("맵 매니저", mapManager, typeof(ChunkBasedMapManager), true);
        if (mapManager != null && GUILayout.Button("매핑 자동 설정", GUILayout.Width(120)))
        {
            AutoSetupMapping();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 검색창
        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField("모듈 이름 검색", searchText);
        showAll = EditorGUILayout.Toggle("모두 표시", showAll, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 모듈 목록 표시
        if (foundModules.Count > 0)
        {
            EditorGUILayout.LabelField($"발견된 모듈: {foundModules.Count}개", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            for (int i = 0; i < foundModules.Count; i++)
            {
                RoomModule module = foundModules[i];
                string guid = moduleGuids[i];
                
                // 검색어 필터링
                if (!showAll && !string.IsNullOrEmpty(searchText) && 
                    !module.name.ToLower().Contains(searchText.ToLower()))
                {
                    continue;
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(module, typeof(RoomModule), false);
                EditorGUILayout.TextField(guid, GUILayout.Width(240));
                
                if (GUILayout.Button("복사", GUILayout.Width(60)))
                {
                    EditorGUIUtility.systemCopyBuffer = guid;
                    Debug.Log($"GUID '{guid}' 복사됨 (모듈: {module.name})");
                }
                
                // 매핑 버튼 - 맵 매니저가 선택된 경우에만 표시
                if (mapManager != null && GUILayout.Button("매핑추가", GUILayout.Width(80)))
                {
                    AddMappingToManager(guid, module);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("'찾기' 버튼을 눌러 모듈을 검색하세요.", MessageType.Info);
        }
    }

    private void FindAllModules()
    {
        foundModules.Clear();
        moduleGuids.Clear();
        
        // 지정된 폴더 내의 모든 RoomModule 에셋 찾기
        string[] guids = AssetDatabase.FindAssets("t:RoomModule", new[] { searchFolder });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            
            if (module != null)
            {
                foundModules.Add(module);
                moduleGuids.Add(guid);
            }
        }
        
        Debug.Log($"{foundModules.Count}개의 RoomModule 에셋을 찾았습니다.");
    }
    
    private void AddMappingToManager(string guid, RoomModule module)
    {
        if (mapManager == null || string.IsNullOrEmpty(guid) || module == null)
            return;
        
        // 같은 GUID가 이미 있는지 확인
        bool alreadyExists = false;
        
        foreach (var mapping in mapManager.moduleGuidMappings)
        {
            if (mapping.guid == guid)
            {
                mapping.module = module;
                alreadyExists = true;
                break;
            }
        }
        
        // 새 매핑 추가
        if (!alreadyExists)
        {
            ChunkBasedMapManager.RoomModuleGuidMapping newMapping = new ChunkBasedMapManager.RoomModuleGuidMapping
            {
                guid = guid,
                module = module
            };
            
            mapManager.moduleGuidMappings.Add(newMapping);
        }
        
        EditorUtility.SetDirty(mapManager);
        Debug.Log($"모듈 '{module.name}'의 매핑이 맵 매니저에 추가되었습니다. (GUID: {guid})");
    }
    
    private void AutoSetupMapping()
    {
        if (mapManager == null || foundModules.Count == 0)
        {
            EditorUtility.DisplayDialog("매핑 설정 실패", 
                "맵 매니저가 선택되지 않았거나 모듈을 먼저 찾아야 합니다.", "확인");
            return;
        }
        
        // 모든 모듈 일괄 추가
        int addedCount = 0;
        
        // 맵 매니저의 기존 매핑 저장
        Dictionary<string, ChunkBasedMapManager.RoomModuleGuidMapping> existingMappings = 
            new Dictionary<string, ChunkBasedMapManager.RoomModuleGuidMapping>();
            
        foreach (var mapping in mapManager.moduleGuidMappings)
        {
            if (!string.IsNullOrEmpty(mapping.guid))
            {
                existingMappings[mapping.guid] = mapping;
            }
        }
        
        // 새 매핑 목록 생성
        List<ChunkBasedMapManager.RoomModuleGuidMapping> newMappings = 
            new List<ChunkBasedMapManager.RoomModuleGuidMapping>();
        
        // 기존 매핑 유지하면서 새 모듈 추가
        for (int i = 0; i < foundModules.Count; i++)
        {
            string guid = moduleGuids[i];
            RoomModule module = foundModules[i];
            
            if (existingMappings.TryGetValue(guid, out var existingMapping))
            {
                // 기존 모듈이 없는 경우 업데이트
                if (existingMapping.module == null)
                {
                    existingMapping.module = module;
                    addedCount++;
                }
                
                newMappings.Add(existingMapping);
            }
            else
            {
                // 새 매핑 추가
                ChunkBasedMapManager.RoomModuleGuidMapping newMapping = 
                    new ChunkBasedMapManager.RoomModuleGuidMapping
                    {
                        guid = guid,
                        module = module
                    };
                
                newMappings.Add(newMapping);
                addedCount++;
            }
        }
        
        // 매핑 설정
        mapManager.moduleGuidMappings = newMappings;
        mapManager.enableDirectMapping = true;
        
        EditorUtility.SetDirty(mapManager);
        Debug.Log($"{addedCount}개의 모듈 매핑이 맵 매니저에 자동 설정되었습니다.");
        
        EditorUtility.DisplayDialog("매핑 설정 완료", 
            $"{addedCount}개의 모듈 매핑이 맵 매니저에 자동 설정되었습니다.", "확인");
    }
} 