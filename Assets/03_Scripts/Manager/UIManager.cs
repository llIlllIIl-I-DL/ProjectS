using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UIManager : Singleton<UIManager> //조금 리팩토링 필요!!
{
    [Header("GameOver Window")]
    public float fadeSpeed = 1.5f;

    [SerializeField] public GameObject gameOverWindow;
    [SerializeField] public Transform gameOverWindowParents;
    [SerializeField] public CanvasGroup fadeOut;

    [SerializeField] public UtilityItemList utilityItemList;

    public List<GameObject> allUIPages = new List<GameObject>();

    InputUI inputUI;
    Player player;

    GameObject _gameOverWindow; //
    CanvasGroup _fadeOut; //

    //보통 Manager에 들어가는 기능 = (UIManager라 했을 때) UI를 가지고 찾고 전달하고...show add remove 등등

    private void Start()
    {
        inputUI = FindObjectOfType<InputUI>();
        player = FindObjectOfType<Player>();

        utilityItemList.GetUtility(player); //플레이어의 특성 15개 아이템 데이터를 담아두는 리스트
    }

    public void CloseAllPage() //모든 UI 창은 뒤로가기 버튼 뿐만 아니라 ESC를 눌렀을 때에도 false가 됨
    {
        foreach (GameObject uiPage in allUIPages)
        {
            uiPage.SetActive(false);
            Time.timeScale = 1;
        }
    }

    public void YouAreOnlyOne(GameObject menu) //인게임 중 단 하나의 UI Canvas만 활성화되도록 함.
    {
        inputUI.currentPage = menu;

        bool isActive = menu.activeSelf;

        if (inputUI.currentPage != null)
        {
            foreach (GameObject uiPage in allUIPages)
            {
                if (uiPage != null)
                {
                    uiPage.gameObject.SetActive(false);
                }
            }

            inputUI.currentPage.SetActive(!isActive);
            Time.timeScale = isActive ? 1 : 0;
        }
    }


    public void ShowGameOverUI()
    {
        _fadeOut = Instantiate(fadeOut, gameOverWindowParents);
        _fadeOut.alpha = 0;

        StartCoroutine(FadeOut(_fadeOut));
    }

    IEnumerator FadeOut(CanvasGroup _fadeOut)
    {
        float alpha = _fadeOut.alpha;

        yield return new WaitForSeconds(1);

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;

            float temp = _fadeOut.alpha;
            temp = alpha;
            _fadeOut.alpha = temp;

            yield return null;
        }


        if (gameOverWindow != null)
        {
            _gameOverWindow = Instantiate(gameOverWindow, gameOverWindowParents);

            yield return new WaitForSeconds(3);

            ToStartMenu(_gameOverWindow);
        }
    }

    public void ToStartMenu(GameObject _gameOverWindow) //스타트 씬으로 이동!!
    {
        SceneManager.LoadScene("TempStartScene", LoadSceneMode.Single);

        Debug.Log("스타트씬 이동!!");
    }
}