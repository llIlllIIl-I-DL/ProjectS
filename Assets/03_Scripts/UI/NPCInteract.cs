using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class NPCInteract : MonoBehaviour
{
    [SerializeField] private GameObject talkBox;

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
    }

    public void ClosedShowTalkBox(Sprite fIcon)
    {
        UIManager.Instance.playerInputHandler.IsInteracting = false;

        Time.timeScale = 1;

        Destroy(_talkBox);
    }
}
