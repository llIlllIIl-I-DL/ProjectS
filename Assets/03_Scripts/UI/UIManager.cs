using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public List<GameObject> allUIPages = new List<GameObject>();

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
                Time.timeScale = 1f;
            }
        }
    }
}
