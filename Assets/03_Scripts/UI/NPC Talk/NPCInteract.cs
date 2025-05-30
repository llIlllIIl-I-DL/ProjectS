using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VInspector.Libs;


public class NPCInteract : MonoBehaviour
{
    [SerializeField] private GameObject talkBox;

    [SerializeField] private List<TextMeshProUGUI> dialogues = new List<TextMeshProUGUI>();
    private int contextCount = 0;

    private bool isTalkOver = true;
    public bool IsTalkOver => isTalkOver;

    private Image faceIcon;
    private GameObject _talkBox;


    public void ShowTalkBox(Sprite fIcon)
    {
        UIManager.Instance.playerInputHandler.IsInteracting = true;

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

        dialogues.Clear();
        dialogues.AddRange(_talkBox.GetComponentsInChildren<TextMeshProUGUI>());

        foreach (var dig in dialogues)
        {
            dig.gameObject.SetActive(false);
        }


        if (contextCount == 0)
        {
            dialogues[0].gameObject.SetActive(true);
            isTalkOver = false;
        } 
    }

    public void NextDialogue()
    {
        if (isTalkOver) return;


        dialogues[contextCount].gameObject.SetActive(false);

        contextCount++;

        if (contextCount < dialogues.Count)
        {
            dialogues[contextCount].gameObject.SetActive(true);
        }

        else
        {
            isTalkOver = true;
            ClosedShowTalkBox();
        }
    }


    public void ClosedShowTalkBox()
    {
        contextCount = 0;

        UIManager.Instance.playerInputHandler.IsInteracting = false;
        Time.timeScale = 1;

        Destroy(_talkBox);
    }
}
