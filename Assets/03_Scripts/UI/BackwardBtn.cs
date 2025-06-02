using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackwardBtn : MonoBehaviour
{
    [SerializeField] public Button backward;
    [SerializeField] public GameObject thisWindow;

    private Canvas currentPage;

    // Start is called before the first frame update
    void Start()
    {
        currentPage = GetComponentInParent<Canvas>();

        backward.onClick.AddListener(() => Backward());
    }

    public void Backward()
    {
        if (currentPage.tag == "Setting")
        {
            thisWindow.SetActive(false);
            Time.timeScale = 0f;
        }

        else
        {
            thisWindow.SetActive(false);
            Time.timeScale = 1f;
        }
    }

}
