using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 모듈 데이터를 저장하기 위한 스크립터블 오브젝트
[CreateAssetMenu(fileName = "New Room Module", menuName = "Metroidvania/Room Module")]
public class RoomModule : ScriptableObject
{
    // 에셋 GUID 저장 필드 (런타임에 GUID로 매핑하기 위해)
    public string assetGUID;

    public GameObject modulePrefab;
    public Texture2D thumbnail;
    public ModuleCategory category;
    public EnvironmentTheme theme;
    public ConnectionPoint[] connectionPoints;
    public bool isSpecialRoom; // 보스룸, 아이템룸 등 특별한 방인지
    
    // 프리팹의 실제 크기 (에디터에서 모듈 배치 시 사용)
    public Vector2 moduleSize = new Vector2(4f, 2f); // 모듈 크기 (1모듈 = 유니티 타일맵 10칸)
    
    // 수동으로 크기 조정 가능 여부
    public bool useCustomSize = false;

    // 표준 타일 크기 상수
    public const float TILE_WIDTH = 1.0f;  // 모듈의 1칸 크기
    public const float TILE_HEIGHT = 1.0f; // 모듈의 1칸 크기
    public const int STANDARD_ROOM_WIDTH_TILES = 4;  // 표준 룸의 가로 타일 수 (4타일 = 40유니티 타일)
    public const int STANDARD_ROOM_HEIGHT_TILES = 2; // 표준 룸의 세로 타일 수 (2타일 = 20유니티 타일)
    
    // 모듈-유니티타일 변환 비율
    public const int UNITY_TILES_PER_MODULE_TILE = 10; // 모듈 1칸 = 유니티 타일맵 10칸

#if UNITY_EDITOR
    // 테스트용 함수 - 유니티 씬에서 오브젝트 배치 시 크기 검증
    [MenuItem("Metroidvania/Test/Create Test Grid In Scene")]
    public static void CreateTestGridInScene()
    {
        GameObject parent = new GameObject("TestModuleGrid");
        
        // 모듈 4x2 크기의 그리드(유니티 타일맵 40x20) 생성
        for (int y = 0; y <= STANDARD_ROOM_HEIGHT_TILES; y++)
        {
            for (int x = 0; x <= STANDARD_ROOM_WIDTH_TILES; x++)
            {
                // 그리드 포인트 표시
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = $"GridPoint_{x}_{y}";
                point.transform.SetParent(parent.transform);
                
                // 위치 설정 - 모듈 타일 크기로 배치
                point.transform.position = new Vector3(x * TILE_WIDTH, y * TILE_HEIGHT, 0);
                point.transform.localScale = Vector3.one * 0.1f;
                
                // 모듈 타일 경계 표시
                if (x < STANDARD_ROOM_WIDTH_TILES && y < STANDARD_ROOM_HEIGHT_TILES)
                {
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"ModuleTile_{x}_{y}";
                    tile.transform.SetParent(parent.transform);
                    
                    // 위치 및 크기 설정
                    tile.transform.position = new Vector3(x * TILE_WIDTH + TILE_WIDTH * 0.5f, 
                                                         y * TILE_HEIGHT + TILE_HEIGHT * 0.5f, 0.1f);
                    tile.transform.localScale = new Vector3(TILE_WIDTH * 0.9f, TILE_HEIGHT * 0.9f, 0.01f);
                    
                    // 색상 설정
                    Renderer renderer = tile.GetComponent<Renderer>();
                    renderer.material.color = new Color(0.2f, 0.8f, 0.8f, 0.3f);
                }
            }
        }
        
        // 유니티 타일맵 스케일 표시
        GameObject unityTileMapGuide = new GameObject("UnityTileMapGuide");
        unityTileMapGuide.transform.SetParent(parent.transform);
        
        // 10x10 유니티 타일 간격으로 포인트 생성
        for (int y = 0; y <= STANDARD_ROOM_HEIGHT_TILES * UNITY_TILES_PER_MODULE_TILE; y += UNITY_TILES_PER_MODULE_TILE)
        {
            for (int x = 0; x <= STANDARD_ROOM_WIDTH_TILES * UNITY_TILES_PER_MODULE_TILE; x += UNITY_TILES_PER_MODULE_TILE)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
                point.name = $"UnityTilePoint_{x}_{y}";
                point.transform.SetParent(unityTileMapGuide.transform);
                
                // 위치 설정 - 유니티 타일맵 스케일로 변환
                float unityX = x * (TILE_WIDTH / UNITY_TILES_PER_MODULE_TILE);
                float unityY = y * (TILE_HEIGHT / UNITY_TILES_PER_MODULE_TILE);
                
                point.transform.position = new Vector3(unityX, unityY, 0.1f);
                point.transform.localScale = Vector3.one * 0.2f;
                
                // 색상 설정
                Renderer renderer = point.GetComponent<Renderer>();
                renderer.material.color = new Color(1.0f, 0.3f, 0.3f, 0.7f);
            }
        }
        
        Debug.Log($"테스트 그리드가 씬에 생성되었습니다. 모듈 크기: {STANDARD_ROOM_WIDTH_TILES}x{STANDARD_ROOM_HEIGHT_TILES}, " +
                 $"유니티 타일맵 크기: {STANDARD_ROOM_WIDTH_TILES * UNITY_TILES_PER_MODULE_TILE}x{STANDARD_ROOM_HEIGHT_TILES * UNITY_TILES_PER_MODULE_TILE}");
        
        // 선택 상태로 설정
        Selection.activeGameObject = parent;
    }
    
    private void OnValidate()
    {
        string path = AssetDatabase.GetAssetPath(this);
        assetGUID = AssetDatabase.AssetPathToGUID(path);
        
        // 프리팹이 설정되어 있고 수동 크기 조정을 사용하지 않을 경우
        if (modulePrefab != null && !useCustomSize)
        {
            CalculateModuleSize();
        }
    }
    
    // 프리팹 크기 계산
    private void CalculateModuleSize()
    {
        // 기본값으로 표준 룸 크기 사용 (모듈 타일 기준)
        Vector2 standardSize = new Vector2(
            STANDARD_ROOM_WIDTH_TILES * TILE_WIDTH,
            STANDARD_ROOM_HEIGHT_TILES * TILE_HEIGHT
        );
        
        // 프리팹에서 크기 정보 가져오기
        Renderer[] renderers = modulePrefab.GetComponentsInChildren<Renderer>();
        Collider2D[] colliders = modulePrefab.GetComponentsInChildren<Collider2D>();
        
        if (renderers.Length > 0 || colliders.Length > 0)
        {
            Bounds bounds = new Bounds(modulePrefab.transform.position, Vector3.zero);
            
            // 렌더러 바운드 통합
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            // 콜라이더 바운드 통합
            foreach (Collider2D collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }
            
            // X, Y 크기만 저장 (2D 게임 기준) - 유니티 타일 크기에서 모듈 타일 크기로 변환
            moduleSize = new Vector2(
                bounds.size.x / UNITY_TILES_PER_MODULE_TILE,
                bounds.size.y / UNITY_TILES_PER_MODULE_TILE
            );
            
            // 만약 크기가 너무 작다면 표준 크기 사용
            if (moduleSize.x < 1f || moduleSize.y < 1f)
            {
                moduleSize = standardSize;
            }
        }
        else
        {
            // 렌더러나 콜라이더가 없을 경우 표준 크기 사용
            moduleSize = standardSize;
        }
    }
    
    // 타일 기반 크기 설정 (편집용)
    public void SetSizeByTiles(int widthInTiles, int heightInTiles)
    {
        moduleSize = new Vector2(
            widthInTiles * TILE_WIDTH,
            heightInTiles * TILE_HEIGHT
        );
        useCustomSize = true;
    }
    
    // 유니티 타일 크기를 모듈 타일 크기로 변환
    public static Vector2 UnityTilesToModuleTiles(Vector2 unityTileSize)
    {
        return new Vector2(
            unityTileSize.x / UNITY_TILES_PER_MODULE_TILE,
            unityTileSize.y / UNITY_TILES_PER_MODULE_TILE
        );
    }
    
    // 모듈 타일 크기를 유니티 타일 크기로 변환
    public static Vector2 ModuleTilesToUnityTiles(Vector2 moduleTileSize)
    {
        return new Vector2(
            moduleTileSize.x * UNITY_TILES_PER_MODULE_TILE,
            moduleTileSize.y * UNITY_TILES_PER_MODULE_TILE
        );
    }
    
    // 모든 모듈에 표준 크기 적용
    [MenuItem("Metroidvania/Set Standard Size to All Modules")]
    public static void SetStandardSizeToAllModules()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomModule");
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(path);
            
            if (module != null)
            {
                module.moduleSize = new Vector2(
                    STANDARD_ROOM_WIDTH_TILES * TILE_WIDTH,
                    STANDARD_ROOM_HEIGHT_TILES * TILE_HEIGHT
                );
                EditorUtility.SetDirty(module);
                count++;
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"표준 크기가 {count}개 모듈에 적용되었습니다.");
    }
#endif

    public enum ModuleCategory
    {
        Combat,
        Puzzle,
        Hub,
        Corridor,
        Village,
        Save,
        Boss,
        Secret
    }
    
    // 환경 테마 열거형 추가
    public enum EnvironmentTheme
    {
        Aether_Dome, // 에테르돔
        Last_Rain,       // 라스트레인
        Waste_Disposal_Plant,     // 폐기물 처리장
        Steel_Mill, // 제철소
        Sewers,    // 하수도
        Thermal_Power_Plant, //화력 발전소
        Central_Cooling_Unit,    // 중앙 냉각장치
    }
}