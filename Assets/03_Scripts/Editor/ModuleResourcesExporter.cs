using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 모듈 에셋을 Resources 폴더로 복사하는 에디터 도구
/// </summary>
public class ModuleResourcesExporter : EditorWindow
{
    [MenuItem("Tools/Modules/Export Modules To Resources")]
    private static void ShowWindow()
    {
        var window = GetWindow<ModuleResourcesExporter>();
        window.titleContent = new GUIContent("모듈 Resources 내보내기");
        window.Show();
    }

    private string searchFolder = "Assets";
    private string resourcesFolderPath = "Assets/Resources/Modules";
    private bool includeSubfolders = true;
    private List<RoomModule> moduleAssets = new List<RoomModule>();
    private Vector2 scrollPos;
    private bool keepOriginalName = true;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("모듈 Resources 내보내기 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "이 도구를 사용하면 프로젝트 내의 모든 RoomModule 에셋을 Resources 폴더로 복사하여 런타임에 로드할 수 있게 합니다.",
            MessageType.Info);
        
        EditorGUILayout.Space();

        // 설정
        searchFolder = EditorGUILayout.TextField("검색 폴더", searchFolder);
        resourcesFolderPath = EditorGUILayout.TextField("대상 Resources 폴더", resourcesFolderPath);
        includeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", includeSubfolders);
        keepOriginalName = EditorGUILayout.Toggle("원본 이름 유지", keepOriginalName);

        EditorGUILayout.Space();
        
        // 모듈 찾기 버튼
        if (GUILayout.Button("모듈 찾기"))
        {
            FindModules();
        }

        // 모듈 목록 표시
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"발견된 모듈: {moduleAssets.Count}개", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var module in moduleAssets)
        {
            EditorGUILayout.ObjectField(module, typeof(RoomModule), false);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 내보내기 버튼
        GUI.enabled = moduleAssets.Count > 0;
        if (GUILayout.Button("모듈을 Resources로 내보내기"))
        {
            ExportModulesToResources();
        }
        GUI.enabled = true;
    }

    private void FindModules()
    {
        moduleAssets.Clear();
        
        // 지정된 폴더 내의 모든 RoomModule 에셋 찾기
        string[] guids = AssetDatabase.FindAssets("t:RoomModule", new[] { searchFolder });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            
            if (module != null)
            {
                moduleAssets.Add(module);
            }
        }
        
        Debug.Log($"{moduleAssets.Count}개의 RoomModule 에셋을 찾았습니다.");
    }

    private void ExportModulesToResources()
    {
        int successCount = 0;
        List<string> failedModules = new List<string>();
        
        // Resources 폴더가 없으면 생성
        if (!Directory.Exists(resourcesFolderPath))
        {
            Directory.CreateDirectory(resourcesFolderPath);
        }
        
        foreach (var module in moduleAssets)
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(module);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                
                // 대상 경로 구성 (GUID 또는 원래 이름 사용)
                string fileName = keepOriginalName ? module.name : guid;
                string targetPath = Path.Combine(resourcesFolderPath, fileName + ".asset");
                
                // 같은 이름의 에셋이 있으면 덮어쓰기
                if (File.Exists(targetPath))
                {
                    AssetDatabase.DeleteAsset(targetPath);
                }
                
                // 에셋 복사
                if (AssetDatabase.CopyAsset(assetPath, targetPath))
                {
                    successCount++;
                }
                else
                {
                    failedModules.Add(module.name);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"모듈 '{module.name}' 내보내기 중 오류 발생: {e.Message}");
                failedModules.Add(module.name);
            }
        }
        
        // 에셋 데이터베이스 갱신
        AssetDatabase.Refresh();
        
        // 매핑 파일도 생성
        GenerateGuidMapping();
        
        string resultMessage = $"{successCount}개 모듈이 성공적으로 내보내졌습니다.";
        if (failedModules.Count > 0)
        {
            resultMessage += $"\n\n{failedModules.Count}개 모듈 내보내기 실패:\n" +
                             string.Join("\n", failedModules);
        }
        
        EditorUtility.DisplayDialog("내보내기 완료", resultMessage, "확인");
    }
    
    private void GenerateGuidMapping()
    {
        // GUID-경로 매핑 생성
        ChunkBasedMapManager.ModuleGuidMappingData mappingData = new ChunkBasedMapManager.ModuleGuidMappingData();
        
        // Resources/Modules 폴더 내의 모든 에셋 검색
        string[] guids = AssetDatabase.FindAssets("t:RoomModule", new[] { resourcesFolderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            if (assetPath.Contains("Resources/"))
            {
                // Resources 폴더 내 상대 경로 추출
                string resourcesPath = assetPath.Substring(assetPath.IndexOf("Resources/") + "Resources/".Length);
                resourcesPath = Path.Combine(Path.GetDirectoryName(resourcesPath), 
                                         Path.GetFileNameWithoutExtension(resourcesPath))
                                 .Replace('\\', '/');
                
                // 모듈의 원래 GUID 찾기
                RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
                
                // 매핑 추가
                ChunkBasedMapManager.ModuleGuidPathMapping mapping = new ChunkBasedMapManager.ModuleGuidPathMapping
                {
                    guid = guid,  // 에셋의 GUID
                    resourcesPath = resourcesPath  // Resources 내 경로
                };
                
                mappingData.mappings.Add(mapping);
            }
        }
        
        // JSON 생성
        string json = JsonUtility.ToJson(mappingData, true);
        
        // JSON 파일 저장 (Resources 폴더에)
        string outputPath = Path.Combine("Assets/Resources", "GuidMapping.json");
        File.WriteAllText(outputPath, json);
        
        AssetDatabase.Refresh();
        Debug.Log($"GUID 매핑 파일이 생성되었습니다: {outputPath}");
        Debug.Log($"{mappingData.mappings.Count}개의 모듈 매핑이 저장되었습니다.");
    }
} 