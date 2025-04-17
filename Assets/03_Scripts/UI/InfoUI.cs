using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    [Header("카테고리 버튼")]
    [SerializeField] public Button typeBtn;
    [SerializeField] public Button utilityBtn;
    [SerializeField] public Button suitBtn;

    [Header("카테고리")]
    [SerializeField] public GameObject typePage;
    [SerializeField] public GameObject utilityPage;
    [SerializeField] public GameObject suitPage;

    [SerializeField] GameObject InfoMenu;
    static GameObject previousPage;

    public void Start()
    {
        previousPage = typePage;

        typeBtn.onClick.AddListener(() => ActivateCategory(typePage));
        utilityBtn.onClick.AddListener(() => ActivateCategory(utilityPage));
        suitBtn.onClick.AddListener(() => ActivateCategory(suitPage));
    }

    public void SetDefaultPage()
    {
        if (InfoMenu.activeSelf == true)
        {
            previousPage = typePage;
            previousPage.SetActive(true);
            utilityPage.SetActive(false);
            suitPage.SetActive(false);
        }
    }

    public void ActivateCategory(GameObject page)
    {
        if (previousPage != null)
        {
            previousPage.SetActive(false);
        }

        bool isOpen = page.activeSelf;
        page.SetActive(!isOpen);


        previousPage = page;
    }
}
