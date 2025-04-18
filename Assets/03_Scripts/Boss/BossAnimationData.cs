using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BossAnimationData
{
    [Header("일반")]
    [SerializeField] private string idleParameterName = "Idle"; //기본
    [SerializeField] private string moveParameterName = "Move"; //이동'

    [Header("공격")]
    [SerializeField] private string pattern1_NormalProjectile = "NomalProjectile"; //일반 투사체
    [SerializeField] private string pattern2_ChargeProjectile = "ChargeProjectile";//차징 투사체
    [SerializeField] private string pattern3_ChargeSlash = "ChargeSlash";//차징 휘두르기
    [SerializeField] private string pattern4_Kick = "Kick";//발차기

    [Header("사망 및 그로기")]
    [SerializeField] private string groggyParameterName = "Groggy";//그로기
    [SerializeField] private string deadParameterName = "Dead";//사망

    public int IdleParameterName { get; private set; }
    public int MoveParameterName { get; private set; }

    public int Pattern1_NormalProjectile { get; private set; }
    public int Pattern2_ChargeProjectile { get; set; }
    public int Pattern3_ChargeSlash { get; set; }
    public int Pattern4_Kick { get; set; }

    public int Groggy { get; private set; }
    public int Dead { get; private set; }

    public void Initialize()
    {
        IdleParameterName = Animator.StringToHash(idleParameterName);
        MoveParameterName = Animator.StringToHash(moveParameterName);

        Pattern1_NormalProjectile = Animator.StringToHash(pattern1_NormalProjectile);
        Pattern2_ChargeProjectile = Animator.StringToHash (pattern2_ChargeProjectile);
        Pattern3_ChargeSlash = Animator.StringToHash (pattern3_ChargeSlash);
        Pattern4_Kick = Animator.StringToHash (pattern4_Kick);

        Groggy = Animator.StringToHash(groggyParameterName);
        Dead = Animator.StringToHash(deadParameterName);
    }

}
