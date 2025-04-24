using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private void Start()
    {
        characterInfoBtn.onClick.AddListener(() => InfoMenu(infoMenu));
        toCheckPointBtn.onClick.AddListener(() => UIInPauseMenu(checkPointMenu));
        settingBtn.onClick.AddListener(() => UIInPauseMenu(settingMenu));

        toMainMenuBtn.onClick.AddListener(() => ToMainMenu());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 열려있는 UI가 있는지 확인.
            if (IsAnyUIActive(true))
            {
                UIManager.Instance.CloseAllPage();
            }
            else
            {
                PauseMenu(pauseMenu);
            }
        }

        if (isPauseMenuOpen) //예외처리. 미리 체크하는 편이 좋당
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            SetMenu(mapMenu);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("I키가 눌렸습니다");
            SetMenu(infoMenu);
            infoUI.SetDefaultPage();
        }
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
        SceneManager.LoadScene("TempMainScene", LoadSceneMode.Single);
    }
}