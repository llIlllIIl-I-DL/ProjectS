using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputUI : MonoBehaviour
{
    static GameObject currentPage = null;

    [Header("UI 창")]
    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject mapMenu;
    [SerializeField] public GameObject InfoMenu;


    [Header("PauseManu Btn")]
    [SerializeField] public Button characterInfoBtn;
    [SerializeField] public Button toCheckPointBtn;
    [SerializeField] public Button settingBtn;
    [SerializeField] public Button toMainMenuBtn;

    [Header("PauseManu UI 창")]
    [SerializeField] public GameObject settingMenu;

    static bool isOpen = false;
    //static bool wannaOpenPageOnPauseMenu = false;
    static bool isPauseMenuOpen = false;


    private void Start()
    {
        characterInfoBtn.onClick.AddListener(() => PauseMenu(InfoMenu));

        toCheckPointBtn.onClick.AddListener(() => CheckPointMenu());
        settingBtn.onClick.AddListener(() => SettingMenu());
        toMainMenuBtn.onClick.AddListener(() => ToMainMenu());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseMenu(pauseMenu);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SetMenu(mapMenu);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            SetMenu(InfoMenu);
        }
    }

    public void SetMenu(GameObject Menu)
    {
        currentPage = Menu;

        if (isPauseMenuOpen == false)
        {
            if (isOpen == false)
            {
                if (currentPage != null)
                {
                    currentPage.SetActive(false);
                    Time.timeScale = 1f;
                }

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
    }


    public void PauseMenu(GameObject Menu)
    {
        isPauseMenuOpen = true;
        //wannaOpenPageOnPauseMenu = true;

        if (isPauseMenuOpen == true)
        {
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
                isPauseMenuOpen = false;
                Time.timeScale = 1f;
            }
        }

        //닫을 때 isPauseMenuOpen false로 바꾸기
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
}

