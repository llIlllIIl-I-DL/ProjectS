using System.Collections;
using System.Collections.Generic;
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
        newBtn.onClick.AddListener(() => NewGame());
        loadBtn.onClick.AddListener(() => LoadMenu());
        optionBtn.onClick.AddListener(() => OptionMenu());
        exitBtn.onClick.AddListener(() => CloseGame());
        
    }

    public void NewGame()
    {
        difficultyMenu.SetActive(true);
        SelectDifficulty();
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
