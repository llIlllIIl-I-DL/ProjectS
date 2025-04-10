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
    [SerializeField] public GameObject CheckPointMenu;
    [SerializeField] public GameObject SettingMenu;
    [SerializeField] public GameObject difficultyMenu;


    void Start()
    {
        newBtn.onClick.AddListener(() => SceneManager.LoadScene("YJ_UI_Scene1", LoadSceneMode.Single));
        
        loadBtn.onClick.AddListener(() => LoadMenu());
        optionBtn.onClick.AddListener(() => OptionMenu());
        exitBtn.onClick.AddListener(() => CloseGame());
        
    }

    public void LoadMenu()
    {
        CheckPointMenu.SetActive(true);
    }


    public void OptionMenu()
    {
        SettingMenu.SetActive(true);
    }

    public void CloseGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting");
    }
}
