using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TagAttribute))]
public class TagSelectorDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif 