using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 상태 열거형
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager instance;
    public static GameManager Instance => instance;

    // 현재 게임 상태
    private GameState currentState;
    public GameState CurrentState => currentState;

    // 게임 매니저 참조들
    private ItemManager itemManager;
    private InventoryManager inventoryManager;
    private CostumeManager costumeManager;
    private WeaponManager weaponManager;
    private AudioManager audioManager;

    // 게임 진행 관련 변수
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private int score = 0;
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private bool isGameInitialized = false;

    // 이벤트 델리게이트
    public delegate void GameStateChangedHandler(GameState newState);
    public event GameStateChangedHandler OnGameStateChanged;

    public delegate void GameScoreChangedHandler(int newScore);
    public event GameScoreChangedHandler OnScoreChanged;

    // 복장 관련 기능 추가
    // 현재 활성화된 복장 세트 ID
    private string activeCostumeId;
    public string ActiveCostumeId => activeCostumeId;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 데이터 로드
        LoadGameData();
        
        // 디버그 상태 출력
        Debug.Log("GameManager 시작됨");
        Debug.Log($"초기 게임 상태: {currentState}");
        
        // 초기 게임 상태를 Playing으로 설정 (UI 상호작용을 위해)
        SetGameState(GameState.Playing);
        
        Debug.Log($"게임 상태를 {CurrentState}로 변경했습니다.");
    }

    private void Update()
    {
        // 게임 중일 때만 게임 시간 업데이트
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }

        // ESC 키로 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape) && (currentState == GameState.Playing || currentState == GameState.Paused))
        {
            TogglePause();
        }


    }

    // 게임 초기화
    private void InitializeGame()
    {
        if (isGameInitialized) return;

        Debug.Log("게임 매니저 초기화 중...");

        // 매니저 참조 가져오기
        itemManager = ItemManager.Instance;
        inventoryManager = InventoryManager.Instance;
        costumeManager = CostumeManager.Instance;
        weaponManager = FindObjectOfType<WeaponManager>();
        audioManager = AudioManager.Instance;

        // 아직 매니저들이 초기화되지 않았을 수 있으므로, 나중에 참조 설정
        StartCoroutine(SetupManagerReferences());

        isGameInitialized = true;
    }

    // 매니저 참조 설정 코루틴
    private IEnumerator SetupManagerReferences()
    {
        // 모든 매니저가 초기화될 때까지 대기
        yield return new WaitForSeconds(0.5f);

        if (itemManager == null) itemManager = ItemManager.Instance;
        if (inventoryManager == null) inventoryManager = InventoryManager.Instance;
        if (costumeManager == null) costumeManager = CostumeManager.Instance;
        if (weaponManager == null) weaponManager = FindObjectOfType<WeaponManager>();
        if (audioManager == null) audioManager = AudioManager.Instance;

        // 이벤트 구독
        if (itemManager != null)
        {
            itemManager.OnItemAdded += OnItemAdded;
            itemManager.OnItemRemoved += OnItemRemoved;
        }

        if (costumeManager != null)
        {
            costumeManager.OnCostumeActivated += OnCostumeActivated;
        }

        Debug.Log("모든 매니저 참조 설정 완료");
    }

    // 이벤트 핸들러들
    private void OnItemAdded(ItemData item)
    {
        // 아이템 획득 시 처리
        Debug.Log($"GameManager: {item.ItemName} 아이템이 추가되었습니다.");
    }

    private void OnItemRemoved(ItemData item)
    {
        // 아이템 제거 시 처리
        Debug.Log($"GameManager: {item.ItemName} 아이템이 제거되었습니다.");
    }

    private void OnCostumeActivated(CostumeSetData costumeSet)
    {
        // 복장 착용 시 처리
        Debug.Log($"GameManager: {costumeSet.costumeName} 복장이 활성화되었습니다.");
    }

    // 게임 상태 설정
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                break;

            case GameState.Victory:
                Time.timeScale = 1f;
                break;
        }

        // 이벤트 발생
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"게임 상태가 {newState}로 변경되었습니다.");
    }

    // 일시정지 토글
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    // 게임 시작
    public void StartGame()
    {
        // 게임 변수 초기화
        gameTime = 0f;
        score = 0;
        playerLevel = 1;

        // 게임 시작 상태로 변경
        SetGameState(GameState.Playing);
    }

    // 게임 오버
    public void GameOver()
    {
        SetGameState(GameState.GameOver);
        Debug.Log("게임 오버!");
    }

    // 게임 승리
    public void Victory()
    {
        SetGameState(GameState.Victory);
        Debug.Log("게임 승리!");
    }

    // 점수 추가
    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    // 현재 게임 시간 가져오기
    public float GetGameTime()
    {
        return gameTime;
    }

    // 현재 점수 가져오기
    public int GetScore()
    {
        return score;
    }

    // 씬 로드
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 씬 다시 로드
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 복장 상태 저장
    public void SaveCostumeState()
    {
        if (costumeManager != null)
        {
            CostumeSetData activeCostume = costumeManager.GetActiveCostume();
            if (activeCostume != null)
            {
                activeCostumeId = activeCostume.costumeId;
                PlayerPrefs.SetString("ActiveCostumeId", activeCostumeId);
                PlayerPrefs.Save();
                Debug.Log($"복장 상태가 저장되었습니다: {activeCostumeId}");
            }
        }
    }

    // 복장 상태 로드
    public void LoadCostumeState()
    {
        if (costumeManager != null)
        {
            string savedCostumeId = PlayerPrefs.GetString("ActiveCostumeId", "");
            if (!string.IsNullOrEmpty(savedCostumeId))
            {
                activeCostumeId = savedCostumeId;
                bool success = costumeManager.ActivateCostume(activeCostumeId);
                if (success)
                {
                    Debug.Log($"저장된 복장 상태 로드 성공: {activeCostumeId}");
                }
                else
                {
                    Debug.LogWarning($"저장된 복장 상태 로드 실패: {activeCostumeId}");
                }
            }
        }
    }

    // 게임 데이터 저장
    public void SaveGameData()
    {
        // 게임 시간, 점수 등 저장
        PlayerPrefs.SetFloat("GameTime", gameTime);
        PlayerPrefs.SetInt("GameScore", score);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        
        // 복장 상태 저장
        SaveCostumeState();
        
        PlayerPrefs.Save();
        Debug.Log("게임 데이터가 저장되었습니다.");
    }

    // 게임 데이터 로드
    public void LoadGameData()
    {
        gameTime = PlayerPrefs.GetFloat("GameTime", 0f);
        score = PlayerPrefs.GetInt("GameScore", 0);
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        
        // 복장 상태 로드
        LoadCostumeState();
        
        Debug.Log("게임 데이터가 로드되었습니다.");
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (itemManager != null)
        {
            itemManager.OnItemAdded -= OnItemAdded;
            itemManager.OnItemRemoved -= OnItemRemoved;
        }

        if (costumeManager != null)
        {
            costumeManager.OnCostumeActivated -= OnCostumeActivated;
        }
    }
} 