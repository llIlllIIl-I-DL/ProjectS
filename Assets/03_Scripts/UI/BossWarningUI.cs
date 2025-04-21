using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BossWarningUI : Singleton<BossWarningUI>
{
    [Header("창")]
    [SerializeField] public GameObject bossWarningUI;
    [SerializeField] public Transform bossWarningUIParents;

    [Header("선택 버튼")]
    [SerializeField] public Button[] Btn;
    //[SerializeField] public Button[] noBtn;

    [HideInInspector] GameObject _bossWarningUI;
    [HideInInspector] public bool isApproved;

    [SerializeField] public PlayerInputHandler PlayerInputHandler;

    GameObject _interactor;

    public void BossWarningWindowUI(GameObject interactor)
    {
        _bossWarningUI = Instantiate(bossWarningUI, bossWarningUIParents);
        _interactor = interactor;

        Btn = _bossWarningUI.GetComponentsInChildren<Button>();

        Btn[0].onClick.AddListener(() => YesYesYes());
        Btn[1].onClick.AddListener(() => NoNoNo());

        Time.timeScale = 0f;
    }

    public void YesYesYes()
    {
        isApproved = true;

        DestroyUI(isApproved);
    }

    public void NoNoNo()
    {
        isApproved = false;

        DestroyUI(isApproved);
    }

    public void DestroyUI(bool isApproved)
    {
        Destroy(_bossWarningUI);
        Time.timeScale = 1f;

        PlayerInputHandler.OnEntrance(_interactor, isApproved);
    }
}
