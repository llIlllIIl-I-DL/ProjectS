using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneUI : MonoBehaviour
{
    [SerializeField] public Button newBtn;
    [SerializeField] public Button loadBtn;
    [SerializeField] public Button optionBtn;
    [SerializeField] public Button exitBtn;


    void Start()
    {
        newBtn.onClick.AddListener(() => SceneManager.LoadScene("YJ_UI_Scene1", LoadSceneMode.Single));
        /*
        loadBtn.onClick.AddListener(() => SceneManager.LoadScene(, LoadSceneMode.Single));
        optionBtn.onClick.AddListener(() => SceneManager.LoadScene(, LoadSceneMode.Single));
        exitBtn.onClick.AddListener(() => SceneManager.LoadScene(, LoadSceneMode.Single));
        */
    }
}
