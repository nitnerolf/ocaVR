// Used in OcaInteractable

using System;

public enum ElementType
{
    Label,
    Toggle,
    Slider,
}

[Serializable]
public class HUDElementDescriptor
{
    public ElementType elementType;
    public string fieldName;
    public string displayName;
    // <summary>
    // Provide fieldName as is, otherwise the reflection system will not be able to find it
    // <summary>
    public HUDElementDescriptor(ElementType type, string exactFieldName, string displayName = null)
    {
        this.elementType = type;
        this.fieldName = exactFieldName;
        this.displayName = displayName;
    }
}