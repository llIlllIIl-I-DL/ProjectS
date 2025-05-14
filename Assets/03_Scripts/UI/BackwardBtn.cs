using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 버튼 기반 - 너무 작은 범위???
public class BackwardBtn : MonoBehaviour
{
    [SerializeField] public Button backward;
    [SerializeField] public GameObject thisWindow;

    // Start is called before the first frame update
    void Start()
    {
        backward.onClick.AddListener(() => Backward());
    }

    public void Backward()
    {
        thisWindow.SetActive(false);
        Time.timeScale = 1f;
    }

}
