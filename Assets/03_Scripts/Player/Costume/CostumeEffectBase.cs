// 복장 효과 베이스 클래스
using UnityEngine;

public abstract class CostumeEffectBase : MonoBehaviour
{
    public CostumeSetData costumeData;

    public abstract void ActivateEffect();
    public abstract void DeactivateEffect();
}