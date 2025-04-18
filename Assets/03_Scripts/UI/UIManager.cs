using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UIManager : Singleton<UIManager>
{

    [Header("GameOver Window")]
    public float fadeSpeed = 1.5f;
    [SerializeField] public GameObject gameOverWindow;
    [SerializeField] public Transform gameOverWindowParents;
    [SerializeField] public Image fadeOut;

    public List<GameObject> allUIPages = new List<GameObject>();

    float fadeOutAlpha;

    InputUI inputUI;
    GameObject _gameOverWindow;

    private void Awake()
    {
        DontDestroyOnLoad(fadeOut);
    }

    private void Start()
    {
        inputUI = FindObjectOfType<InputUI>();

        fadeOut.color = new Color(0, 0, 0, 0);

    }

    public void CloseAllPage()
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
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float alpha = fadeOut.color.a;

        yield return new WaitForSeconds(1);

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;

            Color temp = fadeOut.color;
            temp.a = alpha;
            fadeOut.color = temp;

            yield return null;
        }


        if (gameOverWindow != null)
        {
            _gameOverWindow = Instantiate(gameOverWindow, gameOverWindowParents);
            DontDestroyOnLoad(_gameOverWindow);

            yield return new WaitForSeconds(3);

            ToStartMenu(_gameOverWindow);
        }
    }

    public void ToStartMenu(GameObject _gameOverWindow)
    {
        SceneManager.LoadScene("TempStartScene", LoadSceneMode.Single);
        StartSceneController.Instance.DestroyGameOverWindow(_gameOverWindow, fadeOut);

        Debug.Log("스타트씬 이동!!");
    }
}