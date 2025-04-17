using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AttributeTypes
{
    Normal,
    Rust,
    Iron,
    Poison,
    Water,
    Flame,
    Ice
}

[CreateAssetMenu(fileName = "AttributeType", menuName = "AttributeType")]
public class AttributeTypeData : ScriptableObject
{
    [SerializeField] private string TypeName;
    public string typeName { get { return TypeName; } set { TypeName = value; } }

    public AttributeTypes type;

    [SerializeField] private Sprite TypeIcon;
    public Sprite typeIcon { get { return TypeIcon; } set { TypeIcon = value; } }
}
