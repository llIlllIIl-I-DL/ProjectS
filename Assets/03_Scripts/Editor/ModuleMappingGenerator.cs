using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 모듈의 GUID와 Resources 경로를 매핑하는 파일을 생성하는 에디터 도구
/// </summary>
public class ModuleMappingGenerator : EditorWindow
{
    [MenuItem("Tools/Modules/Generate GUID Mapping")]
    private static void ShowWindow()
    {
        var window = GetWindow<ModuleMappingGenerator>();
        window.titleContent = new GUIContent("모듈 매핑 생성기");
        window.Show();
    }

    private string resourcesFolderPath = "Assets/Resources";
    private string outputFile = "GuidMapping.json";
    private bool includeSubfolders = true;
    private List<RoomModule> moduleAssets = new List<RoomModule>();
    private Vector2 scrollPos;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("모듈 GUID-경로 매핑 생성기", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 설정
        resourcesFolderPath = EditorGUILayout.TextField("Resources 폴더 경로", resourcesFolderPath);
        outputFile = EditorGUILayout.TextField("출력 파일 이름", outputFile);
        includeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", includeSubfolders);

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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(module, typeof(RoomModule), false);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(module));
            EditorGUILayout.TextField(guid);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 매핑 생성 버튼
        GUI.enabled = moduleAssets.Count > 0;
        if (GUILayout.Button("매핑 파일 생성"))
        {
            GenerateMappingFile();
        }
        GUI.enabled = true;
    }

    private void FindModules()
    {
        moduleAssets.Clear();
        
        // Resources 폴더 내의 모든 RoomModule 에셋 찾기
        string[] guids = AssetDatabase.FindAssets("t:RoomModule", new[] { resourcesFolderPath });
        
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

    private void GenerateMappingFile()
    {
        // GUID-경로 매핑 생성
        ChunkBasedMapManager.ModuleGuidMappingData mappingData = new ChunkBasedMapManager.ModuleGuidMappingData();
        
        foreach (var module in moduleAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(module);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            
            // Resources 폴더 경로 변환
            string resourcesPath = GetResourcesPath(assetPath);
            
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                ChunkBasedMapManager.ModuleGuidPathMapping mapping = new ChunkBasedMapManager.ModuleGuidPathMapping
                {
                    guid = guid,
                    resourcesPath = resourcesPath
                };
                
                mappingData.mappings.Add(mapping);
            }
        }
        
        // JSON 생성
        string json = JsonUtility.ToJson(mappingData, true);
        
        // JSON 파일 저장 (Resources 폴더에)
        string resourcesDir = resourcesFolderPath;
        
        // Resources 폴더가 없으면 생성
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }
        
        // 파일 저장
        string outputPath = Path.Combine(resourcesDir, outputFile);
        File.WriteAllText(outputPath, json);
        
        AssetDatabase.Refresh();
        Debug.Log($"GUID 매핑 파일이 생성되었습니다: {outputPath}");
        Debug.Log($"{mappingData.mappings.Count}개의 모듈 매핑이 저장되었습니다.");
        
        EditorUtility.DisplayDialog("매핑 생성 완료", 
            $"{mappingData.mappings.Count}개의 모듈 매핑이 생성되었습니다.\n" +
            "빌드 전에 Assets > Resources 폴더를 확인하세요.", 
            "확인");
    }

    // 에셋 경로를 Resources 폴더 기준 상대 경로로 변환
    private string GetResourcesPath(string assetPath)
    {
        int resourcesIndex = assetPath.IndexOf("Resources/");
        if (resourcesIndex < 0)
        {
            return null; // Resources 폴더 내에 없는 에셋
        }
        
        string relativePath = assetPath.Substring(resourcesIndex + "Resources/".Length);
        
        // 확장자 제거
        relativePath = Path.Combine(Path.GetDirectoryName(relativePath), 
                                  Path.GetFileNameWithoutExtension(relativePath));
        
        // 경로 구분자를 슬래시로 통일
        relativePath = relativePath.Replace('\\', '/');
        
        return relativePath;
    }
} 