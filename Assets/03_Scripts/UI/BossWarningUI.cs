using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWarningUI : MonoBehaviour
{
    [SerializeField] public GameObject bossWarningUI;
    [SerializeField] public Transform bossWarningUIParents;

    public void BossWarningWindowUI()
    {
        GameObject bossWarningWindowUI = bossWarningUI;
        Instantiate(bossWarningWindowUI, bossWarningUIParents);
    }
}
