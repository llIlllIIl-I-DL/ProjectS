using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private SerializedProperty weaponNameProp;
    private SerializedProperty weaponSpriteProp;
    private SerializedProperty weaponDescriptionProp;
    
    private SerializedProperty damageProp;
    private SerializedProperty fireRateProp;
    private SerializedProperty maxAmmoProp;
    private SerializedProperty reloadTimeProp;
    
    private SerializedProperty bulletPrefabProp;
    private SerializedProperty bulletSpeedProp;
    private SerializedProperty bulletLifetimeProp;
    
    private SerializedProperty canChargeProp;
    private SerializedProperty maxChargeTimeProp;
    private SerializedProperty overchargeThresholdProp;
    private SerializedProperty chargedDamageMultiplierProp;
    private SerializedProperty overchargeDamageMultiplierProp;
    private SerializedProperty chargedSizeMultiplierProp;
    private SerializedProperty overchargeSizeMultiplierProp;
    private SerializedProperty overchargePlayerDamagePercentProp;

    private bool showBasicInfo = true;
    private bool showWeaponStats = true;
    private bool showBulletSettings = true;
    private bool showChargeSettings = true;

    private void OnEnable()
    {
        // 무기 기본 정보
        weaponNameProp = serializedObject.FindProperty("weaponName");
        weaponSpriteProp = serializedObject.FindProperty("weaponSprite");
        weaponDescriptionProp = serializedObject.FindProperty("weaponDescription");
        
        // 무기 속성
        damageProp = serializedObject.FindProperty("damage");
        fireRateProp = serializedObject.FindProperty("fireRate");
        maxAmmoProp = serializedObject.FindProperty("maxAmmo");
        reloadTimeProp = serializedObject.FindProperty("reloadTime");
        
        // 투사체 설정
        bulletPrefabProp = serializedObject.FindProperty("bulletPrefab");
        bulletSpeedProp = serializedObject.FindProperty("bulletSpeed");
        bulletLifetimeProp = serializedObject.FindProperty("bulletLifetime");
        
        // 차징 설정
        canChargeProp = serializedObject.FindProperty("canCharge");
        maxChargeTimeProp = serializedObject.FindProperty("maxChargeTime");
        overchargeThresholdProp = serializedObject.FindProperty("overchargeThreshold");
        chargedDamageMultiplierProp = serializedObject.FindProperty("chargedDamageMultiplier");
        overchargeDamageMultiplierProp = serializedObject.FindProperty("overchargeDamageMultiplier");
        chargedSizeMultiplierProp = serializedObject.FindProperty("chargedSizeMultiplier");
        overchargeSizeMultiplierProp = serializedObject.FindProperty("overchargeSizeMultiplier");
        overchargePlayerDamagePercentProp = serializedObject.FindProperty("overchargePlayerDamagePercent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        WeaponData weaponData = (WeaponData)target;
        
        EditorGUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        EditorGUILayout.LabelField("무기 데이터 에디터", titleStyle);
        EditorGUILayout.Space(5);
        
        // 무기 기본 정보
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "무기 기본 정보", true, EditorStyles.foldoutHeader);
        if (showBasicInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(weaponNameProp);
            EditorGUILayout.PropertyField(weaponSpriteProp);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (weaponData.weaponSprite != null)
            {
                GUILayout.Box(weaponData.weaponSprite.texture, GUILayout.Width(64), GUILayout.Height(64));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(weaponDescriptionProp, GUILayout.Height(60));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(5);
        
        // 무기 속성
        showWeaponStats = EditorGUILayout.Foldout(showWeaponStats, "무기 속성", true, EditorStyles.foldoutHeader);
        if (showWeaponStats)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(damageProp);
            EditorGUILayout.PropertyField(fireRateProp);
            EditorGUILayout.PropertyField(maxAmmoProp);
            EditorGUILayout.PropertyField(reloadTimeProp);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(5);
        
        // 투사체 설정
        showBulletSettings = EditorGUILayout.Foldout(showBulletSettings, "투사체 설정", true, EditorStyles.foldoutHeader);
        if (showBulletSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(bulletPrefabProp);
            EditorGUILayout.PropertyField(bulletSpeedProp);
            EditorGUILayout.PropertyField(bulletLifetimeProp);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(5);
        
        // 차징 설정
        showChargeSettings = EditorGUILayout.Foldout(showChargeSettings, "차징 설정", true, EditorStyles.foldoutHeader);
        if (showChargeSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(canChargeProp);
            
            // 차징 기능이 활성화된 경우에만 차징 관련 속성 표시
            if (weaponData.canCharge)
            {
                EditorGUILayout.PropertyField(maxChargeTimeProp);
                EditorGUILayout.PropertyField(overchargeThresholdProp);
                EditorGUILayout.PropertyField(chargedDamageMultiplierProp);
                EditorGUILayout.PropertyField(overchargeDamageMultiplierProp);
                EditorGUILayout.PropertyField(chargedSizeMultiplierProp);
                EditorGUILayout.PropertyField(overchargeSizeMultiplierProp);
                EditorGUILayout.PropertyField(overchargePlayerDamagePercentProp);
                
                // 유효성 검사: overchargeThreshold는 maxChargeTime보다 작아야 함
                if (weaponData.overchargeThreshold > weaponData.maxChargeTime)
                {
                    EditorGUILayout.HelpBox("오버차지 임계값은 최대 차지 시간보다 작아야 합니다!", MessageType.Error);
                }
            }
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
} 