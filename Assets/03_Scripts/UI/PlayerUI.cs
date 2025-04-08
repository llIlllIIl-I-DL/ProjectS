using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("다 꺼져")]
    [SerializeField] public Scrollbar healthBar;

    public void Start()
    {
        Ouch();
    }

    public void Ouch()
    {
        healthBar.size = 0.5f;
    }

}
