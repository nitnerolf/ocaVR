using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ElementType
{
    Label,
    Toggle,
    Slider,
}

[Serializable]
public class GuiElementDescriptor
{
    public ElementType elementType;
    public string fieldName;
    public string displayName;
    // <summary>
    // Provide fieldName as is, otherwise the reflection system will not be able to find it
    // <summary>
    public GuiElementDescriptor(ElementType type, string exactFieldName, string displayName = null)
    {
        this.elementType = type;
        this.fieldName = exactFieldName;
        this.displayName = displayName;
    }
}

public class OcaInteractable : MonoBehaviour
{
    // maps fields to ui elements
    [HideInInspector]
    public List<GuiElementDescriptor> guiElementsDescriptor;
    public OcaControllerHUD HUD;

    private bool IsValidFieldName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName) || string.IsNullOrWhiteSpace(fieldName))
        {
            Debug.LogError("FieldName is empty or invalid");
            return false;
        }

        if (this.GetType().GetField(fieldName) == null)
        {
            Debug.LogError("Cannot find field named '" + fieldName + "'");
            return false;
        }

        return true;
    }

    public object GetValueByFieldName(string fieldName)
    {

        if (!IsValidFieldName(fieldName))
            return null;

        return this.GetType().GetField(fieldName).GetValue(this);
    }

    public void InjectHUDReference(OcaControllerHUD HUD) {
        this.HUD = HUD;
    }
}