using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputUI : MonoBehaviour
{
    public GameObject currentPage;

    [Header("UI 창")]
    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject mapMenu;
    [SerializeField] public GameObject InfoMenu;

    [Header("PauseManu Btn")]
    [SerializeField] public Button characterInfoBtn;
    [SerializeField] public Button toCheckPointBtn;
    [SerializeField] public Button settingBtn;
    [SerializeField] public Button toMainMenuBtn;

    /*
    static bool isPauseManuOpen = false;
    static bool isMapManuOpen = false;
    static bool isInventoryMenuOpen = false;
    */

    static bool isOpen = false;

    private void Start()
    {
        characterInfoBtn.onClick.AddListener(() => SetMenu(InfoMenu));
        toCheckPointBtn.onClick.AddListener(() => CheckPointMenu());
        settingBtn.onClick.AddListener(() => SettingMenu());
        toMainMenuBtn.onClick.AddListener(() => ToMainMenu());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenu(pauseMenu);    //SetPauseMenu();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SetMenu(mapMenu);   //SetMapMenu();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            SetMenu(InfoMenu);
        }
    }

    public void SetMenu(GameObject Menu)
    {
        currentPage = Menu;

        if (isOpen == false)
        {

            Menu.SetActive(true);
            isOpen = true;
            Time.timeScale = 0f;
        }

        else
        {
            Menu.SetActive(false);
            isOpen = false;
            Time.timeScale = 1f;
        }
    }




    public void CheckPointMenu()
    {

    }

    public void SettingMenu()
    {

    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene("YJ_UI_Scene", LoadSceneMode.Single);
    }

    /*
    public void SetPauseMenu()
    {
        if (isPauseManuOpen == false)
        {
            pauseMenu.SetActive(true);
            isPauseManuOpen = true;
            Time.timeScale = 0f;
        }

        else
        {
            pauseMenu.SetActive(false);
            isPauseManuOpen = false;
            Time.timeScale = 1f;
        }
    }

    public void SetMapMenu()
    {
        if (isMapManuOpen == false)
        {
            mapMenu.SetActive(true);
            isMapManuOpen = true;
            Time.timeScale = 0f;
        }

        else
        {
            mapMenu.SetActive(false);
            isMapManuOpen = false;
            Time.timeScale = 1f;
        }
    }
    */


}

