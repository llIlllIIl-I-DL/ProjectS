using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputUI : MonoBehaviour
{
    //UIManager에서 currentPage를 만들고 현재 Active된 캔버스를 할당 한 뒤 null이 아닐 때 timescale 0으로 하기??....


    public GameObject currentPage = null;  //여기를 인식해서 같은 키 눌렀을 때 켜고 끄기.

    //List로 변경하고 순회하면서 현재 Active된 캔버스 제외 전부 false
    
    /// <summary>
    /// 창 켠 상태일때마다 시간 똑바로 멈춰있게 만들기만 하면 된다~~
    /// </summary>

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
            PauseMenu();

        }

        if (isPauseMenuOpen) //예외처리 미리 체크하는 편이 좋당
            return;

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

        UIManager.Instance.YouAreOnlyOne(menu);

        currentPage = menu;

    }

    public void PauseMenu()
    {
        pauseMenu.SetActive(!isPauseMenuOpen); //토글 작업 시 자주 사용
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
        isPauseMenuOpen = false;
        SetMenu(menu);
    }

    /*
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

    */
    public void ToMainMenu()
    {
        SceneManager.LoadScene("YJ_UI_Scene", LoadSceneMode.Single);
    }
}

