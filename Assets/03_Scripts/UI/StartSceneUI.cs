using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneUI : MonoBehaviour
{
    [Header("StartScene Button")]
    [SerializeField] public Button newBtn;
    [SerializeField] public Button loadBtn;
    [SerializeField] public Button optionBtn;
    [SerializeField] public Button exitBtn;

    [Header("StartScene Menu")]
    [SerializeField] public GameObject checkPointMenu;
    [SerializeField] public GameObject settingMenu;
    [SerializeField] public GameObject difficultyMenu;

    [Header("Difficulty Button")]
    [SerializeField] public Button easy;
    [SerializeField] public Button normal;
    [SerializeField] public Button hard;

    void Start()
    {
        newBtn.onClick.AddListener(() => StartNewGame());
        loadBtn.onClick.AddListener(() => LoadMenu());
        optionBtn.onClick.AddListener(() => OptionMenu());
        exitBtn.onClick.AddListener(() => CloseGame());
    }

    public void StartNewGame()
    {
        // GameManager가 있다면 게임 초기화
        if (GameManager.Instance != null)
        {
            // 게임 데이터 초기화
            GameManager.Instance.StartGame();
            
            // 게임 상태를 Playing으로 설정
            GameManager.Instance.SetGameState(GameState.Playing);
            
            // 씬 로드
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("GameManager를 찾을 수 없습니다!");
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        }
    }

    public void SelectDifficulty()
    {
        easy.onClick.AddListener(() => SceneManager.LoadScene("MainScene", LoadSceneMode.Single));
        normal.onClick.AddListener(() => SceneManager.LoadScene("MainScene", LoadSceneMode.Single));
        hard.onClick.AddListener(() => SceneManager.LoadScene("MainScene", LoadSceneMode.Single));
    }

    public void LoadMenu()
    {
        checkPointMenu.SetActive(true);
    }

    public void OptionMenu()
    {
        settingMenu.SetActive(true);
    }

    public void CloseGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting");
    }
}
