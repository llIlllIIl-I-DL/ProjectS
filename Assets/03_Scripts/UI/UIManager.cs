using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public List<GameObject> allUIPages = new List<GameObject>();

    InputUI inputUI;


    private void Start()
    {
        inputUI = FindObjectOfType<InputUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPage();
        }
    }

    public void CloseAllPage()
    {
        foreach (GameObject uiPage in allUIPages)
        {
            if (uiPage != null)
            {
                uiPage.gameObject.SetActive(false);
            }
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
            Time.timeScale = isActive ? 1 : 0; //삼항연산자
        }
    }
}