using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextInventoryPageBtn : MonoBehaviour
{
    //리스트 혹은 딕셔너리
    //인덱스에서 쁠마 1씩 넘무나 감사...

    [SerializeField] public Button invenSlotLeftBtn;
    [SerializeField] public Button invenSlotRightBtn;

    [SerializeField] public GameObject currentPage;
    /*
    [SerializeField] public GameObject InventoryPage1;
    [SerializeField] public GameObject InventoryPage2;
    [SerializeField] public GameObject InventoryPage3;
    */

    [SerializeField] public List<GameObject> Pages = new List<GameObject>();

    int i = 0;

    void Start()
    {
        invenSlotLeftBtn.onClick.AddListener(() => LeftPage());
        invenSlotRightBtn.onClick.AddListener(() => RightPage());
    }

    public void LeftPage()
    {
        currentPage.SetActive(false);

        currentPage = Pages[i + 2];

        SwapLeft();

        currentPage.SetActive(true);
    }

    public void SwapLeft()
    {
        GameObject Temp = Pages[0];
        Pages[0] = Pages[2];
        Pages[2] = Pages[1];
        Pages[1] = Temp;
    }




    public void RightPage()
    {
        currentPage.SetActive(false);

        currentPage = Pages[i + 1];

        SwapRight();

        currentPage.SetActive(true);

    }

    public void SwapRight()
    {
        GameObject Temp = Pages[1];
        Pages[1] = Pages[2];
        Pages[2] = Pages[0];
        Pages[0] = Temp;
    }










    /*
    public void MoveToLeftPage(GameObject currentPage)
    {
        if (currentPage == InventoryPage1)
        {
            currentPage.SetActive(false);
            InventoryPage3.SetActive(true);

            this.currentPage = InventoryPage3;
        }

        else if (currentPage == InventoryPage3)
        {
            currentPage.SetActive(false);
            InventoryPage2.SetActive(true);

            this.currentPage = InventoryPage2;
        }

        else if (currentPage == InventoryPage2)
        {
            currentPage.SetActive(false);
            InventoryPage1.SetActive(true);

            this.currentPage = InventoryPage1;
        }
    }


    public void MoveToRightPage(GameObject currentPage)
    {
        if (currentPage == InventoryPage1)
        {
            currentPage.SetActive(false);
            InventoryPage2.SetActive(true);

            this.currentPage = InventoryPage2;
        }


        else if (currentPage == InventoryPage2)
        {
            currentPage.SetActive(false);
            InventoryPage3.SetActive(true);

            this.currentPage = InventoryPage3;
        }

        else if (currentPage == InventoryPage3)
        {
            currentPage.SetActive(false);
            InventoryPage1.SetActive(true);

            this.currentPage = InventoryPage1;
        }
    }
    */
}
