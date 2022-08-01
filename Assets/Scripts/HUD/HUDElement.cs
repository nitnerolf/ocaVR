// Used in OcaInteractable

using System;

public enum ElementType
{
    Label,
    Toggle,
    Slider,
}

[Serializable]
public class HUDElement
{
    public ElementType elementType;
    public string fieldName;
    public string displayName;

    // Provide fieldName as is, otherwise the reflection system will not be able to find it
    public HUDElement(ElementType type, string exactFieldName, string displayName = null)
    {
        this.elementType = type;
        this.fieldName = exactFieldName;
        this.displayName = displayName;
    }
}