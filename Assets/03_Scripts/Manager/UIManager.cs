using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager> //조금 리팩토링 필요!!
{
    [SerializeField] public UtilityItemList utilityItemList;

    // UI가 필요한 경우 만들어서 사용할 수 있는 창구면 좋을 것 같다.
    public List<GameObject> allUIPages = new List<GameObject>();

    InputUI inputUI;
    Player player;

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
}