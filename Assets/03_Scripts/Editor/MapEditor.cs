using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class MapEditorWindow : EditorWindow
{
    // 에디터 설정
    private GameObject tilePrefab; // 타일 프리팹
    private GameObject platformPrefab; // 플랫폼 프리팹
    private GameObject enemyPrefab; // 적 프리팹
    private GameObject collectiblePrefab; // 수집품 프리팹

    // 에디터 상태
    private enum PlacementMode
    {
        Tile,
        Platform,
        Enemy,
        Collectible,
        Eraser
    }
    private PlacementMode currentMode = PlacementMode.Tile;

    // 맵 데이터
    private List<MapObject> mapObjects = new List<MapObject>();
    private string mapName = "NewMap";

    [MenuItem("Window/2D Platformer Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("맵 에디터");
    }

    private void OnGUI()
    {
        GUILayout.Label("2D 플랫포머 맵 에디터", EditorStyles.boldLabel);

        // 프리팹 설정
        EditorGUILayout.Space();
        GUILayout.Label("프리팹 설정", EditorStyles.boldLabel);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("타일 프리팹", tilePrefab, typeof(GameObject), false);
        platformPrefab = (GameObject)EditorGUILayout.ObjectField("플랫폼 프리팹", platformPrefab, typeof(GameObject), false);
        enemyPrefab = (GameObject)EditorGUILayout.ObjectField("적 프리팹", enemyPrefab, typeof(GameObject), false);
        collectiblePrefab = (GameObject)EditorGUILayout.ObjectField("수집품 프리팹", collectiblePrefab, typeof(GameObject), false);

        // 배치 모드 선택
        EditorGUILayout.Space();
        GUILayout.Label("배치 모드", EditorStyles.boldLabel);
        currentMode = (PlacementMode)EditorGUILayout.EnumPopup("현재 모드:", currentMode);

        // 맵 이름 및 저장/불러오기
        EditorGUILayout.Space();
        GUILayout.Label("맵 관리", EditorStyles.boldLabel);
        mapName = EditorGUILayout.TextField("맵 이름:", mapName);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("새 맵"))
        {
            if (EditorUtility.DisplayDialog("새 맵 만들기", "모든 배치된 오브젝트가 삭제됩니다. 계속하시겠습니까?", "예", "아니오"))
            {
                ClearMap();
            }
        }

        if (GUILayout.Button("맵 저장"))
        {
            SaveMap();
        }

        if (GUILayout.Button("맵 불러오기"))
        {
            LoadMap();
        }
        EditorGUILayout.EndHorizontal();

        // 사용 방법 안내
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("사용 방법:\n1. 프리팹을 설정하세요.\n2. 배치 모드를 선택하세요.\n3. Scene 뷰에서 Shift+클릭으로 오브젝트를 배치하세요.\n4. 맵을 저장하고 불러올 수 있습니다.", MessageType.Info);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.shift)
        {
            Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            mousePosition = SnapToGrid(mousePosition);

            if (e.button == 0) // 왼쪽 마우스 클릭
            {
                PlaceObject(mousePosition);
                e.Use(); // 이벤트 소비
            }
            else if (e.button == 1) // 오른쪽 마우스 클릭
            {
                RemoveObject(mousePosition);
                e.Use(); // 이벤트 소비
            }
        }
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        // 그리드에 맞추기 (1x1 단위)
        float x = Mathf.Round(position.x);
        float y = Mathf.Round(position.y);
        return new Vector2(x, y);
    }

    private void PlaceObject(Vector2 position)
    {
        // 위치에 이미 오브젝트가 있는지 확인
        MapObject existingObject = mapObjects.Find(obj => Vector2.Distance(obj.Position, position) < 0.1f);
        if (existingObject != null)
        {
            Debug.Log("이미 이 위치에 오브젝트가 있습니다!");
            return;
        }

        GameObject prefabToUse = null;
        string objectType = "";

        switch (currentMode)
        {
            case PlacementMode.Tile:
                prefabToUse = tilePrefab;
                objectType = "Tile";
                break;
            case PlacementMode.Platform:
                prefabToUse = platformPrefab;
                objectType = "Platform";
                break;
            case PlacementMode.Enemy:
                prefabToUse = enemyPrefab;
                objectType = "Enemy";
                break;
            case PlacementMode.Collectible:
                prefabToUse = collectiblePrefab;
                objectType = "Collectible";
                break;
            case PlacementMode.Eraser:
                RemoveObject(position);
                return;
        }

        if (prefabToUse == null)
        {
            Debug.LogWarning($"선택된 모드({currentMode})에 대한 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 프리팹 인스턴스 생성
        GameObject instance = PrefabUtility.InstantiatePrefab(prefabToUse) as GameObject;
        instance.transform.position = position;
        instance.name = $"{objectType}_{position.x}_{position.y}";

        // 맵 오브젝트 목록에 추가
        mapObjects.Add(new MapObject
        {
            Type = objectType,
            Position = position,
            InstanceID = instance.GetInstanceID()
        });

        // 실행 취소 지원을 위한 레코드
        Undo.RegisterCreatedObjectUndo(instance, "Place Object");
    }

    private void RemoveObject(Vector2 position)
    {
        MapObject objectToRemove = mapObjects.Find(obj => Vector2.Distance(obj.Position, position) < 0.5f);
        if (objectToRemove != null)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(objectToRemove.InstanceID) as GameObject;
            if (instance != null)
            {
                Undo.DestroyObjectImmediate(instance);
            }

            mapObjects.Remove(objectToRemove);
        }
    }

    private void ClearMap()
    {
        // 모든 맵 오브젝트 삭제
        foreach (MapObject obj in mapObjects)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(obj.InstanceID) as GameObject;
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
        }

        mapObjects.Clear();
    }

    private void SaveMap()
    {
        // 맵 데이터 구조체 생성
        MapData mapData = new MapData
        {
            MapName = mapName,
            Objects = new List<SerializableMapObject>()
        };

        // 모든 맵 오브젝트를 직렬화 가능한 형태로 변환
        foreach (MapObject obj in mapObjects)
        {
            mapData.Objects.Add(new SerializableMapObject
            {
                Type = obj.Type,
                PositionX = obj.Position.x,
                PositionY = obj.Position.y
            });
        }

        // JSON으로 변환
        string json = JsonUtility.ToJson(mapData, true);

        // 저장 경로 선택
        string path = EditorUtility.SaveFilePanel("맵 저장", Application.dataPath, mapName, "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"맵이 저장되었습니다: {path}");
        }
    }

    private void LoadMap()
    {
        // 불러오기 파일 선택
        string path = EditorUtility.OpenFilePanel("맵 불러오기", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;

        string json = File.ReadAllText(path);
        MapData mapData = JsonUtility.FromJson<MapData>(json);

        if (mapData == null)
        {
            Debug.LogError("맵 데이터를 불러오는데 실패했습니다.");
            return;
        }

        // 기존 맵 정리
        ClearMap();

        // 맵 이름 설정
        mapName = mapData.MapName;

        // 맵 오브젝트 생성
        foreach (SerializableMapObject obj in mapData.Objects)
        {
            Vector2 position = new Vector2(obj.PositionX, obj.PositionY);
            GameObject prefabToUse = null;

            switch (obj.Type)
            {
                case "Tile":
                    prefabToUse = tilePrefab;
                    break;
                case "Platform":
                    prefabToUse = platformPrefab;
                    break;
                case "Enemy":
                    prefabToUse = enemyPrefab;
                    break;
                case "Collectible":
                    prefabToUse = collectiblePrefab;
                    break;
            }

            if (prefabToUse != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabToUse) as GameObject;
                instance.transform.position = position;
                instance.name = $"{obj.Type}_{position.x}_{position.y}";

                // 맵 오브젝트 목록에 추가
                mapObjects.Add(new MapObject
                {
                    Type = obj.Type,
                    Position = position,
                    InstanceID = instance.GetInstanceID()
                });
            }
        }

        Debug.Log($"맵을 불러왔습니다: {mapName}");
    }
}