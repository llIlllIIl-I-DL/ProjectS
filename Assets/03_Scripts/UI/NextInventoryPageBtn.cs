using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextInventoryPageBtn : MonoBehaviour
{
    [SerializeField] public Button invenSlotLeftBtn;
    [SerializeField] public Button invenSlotRightBtn;

    [SerializeField] public List<GameObject> Pages = new List<GameObject>();

    int currentPage = 0;
    int totalPage;

    void Start()
    {
        totalPage = Pages.Count;

        invenSlotLeftBtn.onClick.AddListener(() => LeftPage());
        invenSlotRightBtn.onClick.AddListener(() => RightPage());
    }

    public void LeftPage()
    {
        Pages[currentPage].SetActive(false);

        currentPage --;

        if(currentPage < 0)
        {
            currentPage = totalPage -1;
        }

        Pages[currentPage].SetActive(true);
    }

    public void RightPage()
    {
        Pages[currentPage].SetActive(false);

        currentPage++; //식을 이렇게 하면 값을 영원히 저장한디야...

        if (currentPage >= totalPage)
        {
            currentPage = 0;
        }

        Pages[currentPage].SetActive(true);
    }
}
