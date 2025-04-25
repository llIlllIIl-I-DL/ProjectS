using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class ModuleTemplateCreator : EditorWindow
{
    private GameObject modulePrefab;
    private Texture2D thumbnail;
    private RoomModule.ModuleCategory category;
    private RoomModule.EnvironmentTheme theme;
    private string moduleName = "New Room Module";
    private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();
    private bool isSpecialRoom = false;
    private string modulePath = "Assets/03_Scripts/RoomModules/";

    [MenuItem("Metroidvania/Module Template Creator")]
    public static void ShowWindow()
    {
        // 에디터 윈도우를 열 때 문자열 변수를 사용하여 타이틀 설정
        string windowTitle = "Module Creator";
        EditorWindow window = GetWindow(typeof(ModuleTemplateCreator));
        window.titleContent = new GUIContent(windowTitle);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Create Room Module", EditorStyles.boldLabel);

        moduleName = EditorGUILayout.TextField("Module Name:", moduleName);
        modulePrefab = (GameObject)EditorGUILayout.ObjectField("Module Prefab:", modulePrefab, typeof(GameObject), false);
        thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail:", thumbnail, typeof(Texture2D), false);
        category = (RoomModule.ModuleCategory)EditorGUILayout.EnumPopup("Category:", category);
        theme = (RoomModule.EnvironmentTheme)EditorGUILayout.EnumPopup("Theme:", theme);
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
                GUILayout.EndVertical();
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
        moduleAsset.theme = theme;
        moduleAsset.isSpecialRoom = isSpecialRoom;
        moduleAsset.connectionPoints = connectionPoints.ToArray();

        // 에셋 저장
        string assetPath = modulePath + moduleName + ".asset";
        AssetDatabase.CreateAsset(moduleAsset, assetPath);
        AssetDatabase.SaveAssets();

        // 에셋 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = moduleAsset;

        Debug.Log("Module created at: " + assetPath);
    }
}