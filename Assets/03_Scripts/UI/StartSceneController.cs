using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StartSceneController : Singleton<StartSceneController>
{
    void Start()
    {

    }

    public void DestroyGameOverWindow(GameObject _gameOverWindow, Image fadeOut)
    {
        Destroy(_gameOverWindow);
        Destroy(fadeOut);
    }
}