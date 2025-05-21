using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour
{
    [SerializeField] private GameObject talkBox;

    private Image faceIcon;

    private GameObject _talkBox;

    public void ShowTalkBox(Sprite fIcon)
    {
        Time.timeScale = 0;


        Image[] images = talkBox.GetComponentsInChildren<Image>();
        faceIcon = images[1];

        if (fIcon != null)
        {
            faceIcon.sprite = fIcon;
        }

        else
        {
            _talkBox = null;
        }

        _talkBox = Instantiate(talkBox);
    }

    public void ClosedShowTalkBox(Sprite fIcon)
    {
        Time.timeScale = 1;

        Destroy(_talkBox);
    }
}
