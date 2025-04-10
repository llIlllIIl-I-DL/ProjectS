using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputUI : MonoBehaviour
{
    //UIManager에서 currentPage를 만들고 현재 Active된 캔버스를 할당 한 뒤 null이 아닐 때 timescale 0으로 하기??....


    public GameObject currentPage = null;

    //List로 변경하고 순회하면서 현재 Active된 캔버스 제외 전부 false

    [Header("UI 창")]
    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject mapMenu;
    [SerializeField] public GameObject infoMenu;

    [Header("PauseManu Btn")]
    [SerializeField] public Button characterInfoBtn;
    [SerializeField] public Button toCheckPointBtn;
    [SerializeField] public Button settingBtn;
    [SerializeField] public Button toMainMenuBtn;

    [Header("PauseManu UI 창")]
    [SerializeField] public GameObject settingMenu;
    [SerializeField] public GameObject checkPointMenu;

    public bool isOpen = false;
    public bool isPauseMenuOpen = false;

    private void Start()
    {
        characterInfoBtn.onClick.AddListener(() => InfoMenu());
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
            SetMenu(infoMenu);
        }
    }

    public void SetMenu(GameObject menu)
    {
        currentPage = menu;

        if (isPauseMenuOpen == false)
        {
            if (isOpen == false)
            {
                UIManager.Instance.YouAreOnlyOne(menu);
                /*
                if (currentPage != null)
                {
                    currentPage.SetActive(false);
                }
                */

                //menu.SetActive(true);
                isOpen = true;
            }

            else
            {
                menu.SetActive(false);
                isOpen = false;
            }
        }
    } //매니저에서 관리....

    public void PauseMenu(GameObject menu)
    {
        isPauseMenuOpen = true;

        if (isPauseMenuOpen == true)
        {
            if (isOpen == false)
            {
                menu.SetActive(true);
                isOpen = true;
            }

            else
            {
                menu.SetActive(false);
                isOpen = false;
                isPauseMenuOpen = false;
            }
        }
    }

    public void InfoMenu()
    {
        bool isActive = infoMenu.activeSelf;

        infoMenu.SetActive(!isActive);
    }



    public void CheckPointMenu()
    {
        bool isActive = checkPointMenu.activeSelf;

        checkPointMenu.SetActive(!isActive);
    }

    public void SettingMenu()
    {
        bool isActive = settingMenu.activeSelf;

        settingMenu.SetActive(!isActive);
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene("YJ_UI_Scene", LoadSceneMode.Single);
    }
}

