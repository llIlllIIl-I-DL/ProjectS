using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class InputUI : MonoBehaviour
{
    public GameObject currentPage = null;

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

    public bool isPauseMenuOpen = false;

    [SerializeField] private InfoUI infoUI;
    [SerializeField] private NPC npc;

    private void Start()
    {
        characterInfoBtn.onClick.AddListener(() => InfoMenu(infoMenu));
        toCheckPointBtn.onClick.AddListener(() => UIInPauseMenu(checkPointMenu));
        settingBtn.onClick.AddListener(() => UIInPauseMenu(settingMenu));

        toMainMenuBtn.onClick.AddListener(() => ToMainMenu());
    }

    public void OpenPauseMenu()
    {
        foreach (var page in UIManager.Instance.settingUIPages)
        {
            if (page != null && page.activeSelf)
            {
                return;
            }
        }

        if (npc == null || !npc.istalking)
        {
            // 열려있는 UI가 있는지 확인.
            if (IsAnyUIActive(true))
            {
                UIManager.Instance.CloseAllPage();
                isPauseMenuOpen = false;
            }
            else
            {
                PauseMenu(pauseMenu);
            }
        }
    }

    public void OpenMap()
    {
        if (isPauseMenuOpen)
            return;

            SetMenu(mapMenu);

    }

    public void OpenInventory()
    {
        if (isPauseMenuOpen)
            return;

            Debug.Log("I키가 눌렸습니다");
            SetMenu(infoMenu);
            infoUI.SetDefaultPage();
    }

    bool IsAnyUIActive(bool isfalse)
    {
        foreach (GameObject uiPage in UIManager.Instance.allUIPages)
        {
            if (uiPage != null && uiPage.activeSelf)
                return true;
        }

        return false;
    }

    public void SetMenu(GameObject menu)
    {

        UIManager.Instance.YouAreOnlyOne(menu);

        currentPage = menu;

    }

    public void PauseMenu(GameObject menu)
    {
        pauseMenu.SetActive(!isPauseMenuOpen); //토글 작업 시 자주 사용하는 방식
        Time.timeScale = isPauseMenuOpen ? 1 : 0; //삼항연산자

        isPauseMenuOpen = !isPauseMenuOpen;
    }

    public void UIInPauseMenu(GameObject menu)
    {
        bool isActive = menu.activeSelf;
        menu.SetActive(!isActive);
    }


    public void InfoMenu(GameObject menu)
    {
        isPauseMenuOpen = true;
        PauseMenu(pauseMenu);
        SetMenu(menu);
    }







    public void ToMainMenu()
    {
        //열려있는 모든 UI를 닫습니다.
        UIManager.Instance.CloseAllPage();
        SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
    }
}