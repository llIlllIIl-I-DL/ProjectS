using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class BossWarningUI : MonoBehaviour
{
    public static BossWarningUI Instance { get; private set; }

    [Header("창")]
    [SerializeField] public GameObject bossWarningUI;
    [SerializeField] public Transform bossWarningUIParents;

    [Header("선택 버튼")]
    [SerializeField] public Button[] Btn;
    //[SerializeField] public Button[] noBtn;

    [HideInInspector] public bool isApproved;

    private ObjectValve currentDoor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BossWarningWindowUI(GameObject interactor, ObjectValve door)
    {
        bossWarningUI.SetActive(true);

        currentDoor = door;

        Btn = bossWarningUI.GetComponentsInChildren<Button>();

        Btn[0].onClick.AddListener(() => YesYesYes());
        Btn[1].onClick.AddListener(() => NoNoNo());

        Time.timeScale = 0f;
    }

    public void YesYesYes()
    {
        isApproved = true;

        if (currentDoor != null)
            currentDoor.OpenValve();

        DestroyUI();
    }

    public void NoNoNo()
    {
        isApproved = false;

        DestroyUI();
    }

    public void DestroyUI()
    {
        bossWarningUI.SetActive(false);
        Time.timeScale = 1f;
    }
}
