using UnityEngine;

[CreateAssetMenu(fileName = "DebuffDataSO", menuName = "ScriptableObjects/DebuffDataSO", order = 1)]
public class DebuffDataSO : ScriptableObject
{
    public DebuffType type;
    public float duration;
    public float intensity;  // 디버프 효과의 강도 (%, 0.0-1.0)
    public float tickDamage; // 초당 데미지
    public GameObject visualEffectPrefab;
    public Color tintColor;  // 적용될 색상
    public AudioClip effectSound;
} 