using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextInventoryPageBtn : MonoBehaviour
{
    [SerializeField] public Button invenSlotLeftBtn;
    [SerializeField] public Button invenSlotRightBtn;

    [SerializeField] public GameObject currentPage;
    [SerializeField] public GameObject InventoryPage1;
    [SerializeField] public GameObject InventoryPage2;
    [SerializeField] public GameObject InventoryPage3;

    void Start()
    {
        invenSlotLeftBtn.onClick.AddListener(() => MoveToLeftPage(currentPage));
        invenSlotRightBtn.onClick.AddListener(() => MoveToRightPage(currentPage));
    }

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
}
