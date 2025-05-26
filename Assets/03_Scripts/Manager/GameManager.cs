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
    
    // 목숨 시스템 관련 변수
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives;
    [SerializeField] private float respawnDelay = 2f; // 부활 대기 시간
    [SerializeField] private Transform respawnPoint; // 부활 위치
    private bool isRespawning = false;
    
    // 프로퍼티
    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;

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
            
            // 씬 전환 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;
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

        /*
        // ESC 키로 일시정지 토글
        if (InteractionKeyInput.GetKeyDown(KeyCode.Escape) && (currentState == GameState.Playing || currentState == GameState.Paused))
        {
            TogglePause();
        }
        */
    }

    // 게임 초기화
    private void InitializeGame()
    {
        if (isGameInitialized) return;

        Debug.Log("게임 매니저 초기화 중...");

        // 목숨 초기화
        currentLives = maxLives;
        
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
        currentLives = maxLives;
        isGameInitialized = false;
        
        // 매니저 참조 초기화
        itemManager = null;
        inventoryManager = null;
        costumeManager = null;
        weaponManager = null;
        audioManager = null;
        
        // 매니저 참조 다시 설정
        StartCoroutine(SetupManagerReferences());
        
        // 게임 시작 상태로 변경
        SetGameState(GameState.Playing);
        
        Debug.Log("새로운 게임이 시작되었습니다.");
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

    // 게임 상태 변경 메소드
    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState)
            return;
            
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
        
        // 게임 상태에 따른 처리
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
                Time.timeScale = 1f;
                // 게임 오버 UI 표시
                PlayerUI.Instance.ShowGameOverUI();
                break;
                
            case GameState.Victory:
                Time.timeScale = 1f;
                // 승리 UI 표시
                //UIManager.Instance?.ShowVictoryUI();
                break;
        }
        
        Debug.Log($"게임 상태가 {newState}로 변경되었습니다.");
    }

    // 플레이어 사망 처리
    public void PlayerDied(GameObject player)
    {
        // 이미 부활 처리 중이면 무시
        if (isRespawning) 
        {
            Debug.Log("이미 부활 처리 중입니다. 중복 사망 처리를 무시합니다.");
            return;
        }
        
        Debug.Log("GameManager: 플레이어 사망 처리 시작");
        
        // 목숨 감소
        currentLives--;
        Debug.Log($"플레이어 사망! 남은 목숨: {currentLives}");
        
        if (currentLives <= 0)
        {
            // 목숨이 없으면 게임 오버
            ChangeGameState(GameState.GameOver);
            
            // 게임 오버 UI 표시
            if (PlayerUI.Instance != null)
            {
                PlayerUI.Instance.ShowGameOverUI();
            }
            
            // 게임 데이터 저장
            SaveGameData();
            
            Debug.Log("게임 오버 처리 완료");
            return;
        }
        
        // 목숨이 남아있으면 부활 처리
        StartCoroutine(RespawnPlayer(player));
    }
    
    // 플레이어 부활 코루틴
    private IEnumerator RespawnPlayer(GameObject player)
    {
        isRespawning = true;
        
        // 부활 대기 시간
        yield return new WaitForSeconds(respawnDelay);
        
        // 플레이어가 여전히 존재하는지 확인
        if (player == null)
        {
            Debug.LogError("부활 처리 실패: 플레이어 객체가 존재하지 않습니다.");
            isRespawning = false;
            yield break;
        }
        
        Debug.Log("리스폰 처리 시작: 게임 초기화 및 상태 리셋");
        
        // 게임 초기화
        StartGame();
        
        // 1. 플레이어 체력 초기화
        var playerHP = player.GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            playerHP.ResetHealth();
        }
        
        // 2. 플레이어 위치 리셋
        if (respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            Debug.Log($"플레이어 위치 리셋: {respawnPoint.position}");
        }
        else
        {
            // 지정된 부활 위치가 없으면 초기 위치 또는 기본 위치로 리셋
            player.transform.position = Vector3.zero;
            Debug.Log("플레이어 위치 리셋: (0,0,0)");
        }
        
        // 3. 물리 속성 초기화
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false; // 물리 영향 다시 활성화
            Debug.Log("Rigidbody2D 초기화 완료");
        }
        
        // 4. 콜라이더 다시 활성화
        Collider2D[] colliders = player.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
        Debug.Log("모든 콜라이더 활성화 완료");
        
        // 5. 중요: 애니메이터에서 사망 상태(IsDead) false로 설정
        var playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetDead(false);
            playerAnimator.SetAnimatorSpeed(1.0f); // 애니메이션 속도 정상화
            Debug.Log("애니메이터 사망 상태 해제 및 속도 정상화 완료");
        }
        
        // 6. 플레이어 상태를 Idle로 변경
        var playerStateManager = player.GetComponent<PlayerStateManager>();
        if (playerStateManager != null)
        {
            // 상태 변경 전에 확실히 준비가 되었는지 확인
            yield return new WaitForSeconds(0.1f); // 짧은 대기 시간으로 모든 상태가 리셋될 시간 확보
            
            Debug.Log("플레이어 상태를 Idle로 변경 시작");
            playerStateManager.ChangeState(PlayerStateType.Idle);
            Debug.Log("플레이어 상태 Idle로 변경 완료");
        }
        
        // 게임 상태를 정상 플레이로 변경
        SetGameState(GameState.Playing);
        
        isRespawning = false;
        Debug.Log("플레이어 부활 및 게임 초기화 완료!");
    }

    // 씬 로드 시 호출되는 메서드
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"씬 로드됨: {scene.name}");
        
        // 매니저 참조 다시 설정
        StartCoroutine(SetupManagerReferences());
        
        // 게임 상태 초기화
        if (scene.name == "GameScene") // 게임 씬 이름에 맞게 수정 필요
        {
            SetGameState(GameState.Playing);
        }
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
        
        // 씬 전환 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 플레이어 사망 애니메이션 처리 및 게임 오버
    private IEnumerator ShowDeathAnimation(GameObject player)
    {
        Debug.Log("게임 오버 처리 시작: 사망 애니메이션 재생");
        
        // 플레이어 애니메이터 가져오기
        var playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            // 사망 애니메이션 재생
            playerAnimator.SetDead(true);
            playerAnimator.SetAnimatorSpeed(1.0f);
        }
        
        // 사망 애니메이션 재생 시간 대기 (2초)
        yield return new WaitForSeconds(2f);
        
        // 게임 오버 상태로 변경
        ChangeGameState(GameState.GameOver);
        
        // 게임 오버 UI 표시
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.ShowGameOverUI();
        }
        
        // 게임 데이터 저장
        SaveGameData();
        
        Debug.Log("게임 오버 처리 완료");
    }
} 