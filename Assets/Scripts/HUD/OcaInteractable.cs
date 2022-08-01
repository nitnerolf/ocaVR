/*
    This class is a wrapper for the 'MonoBehaviour' class.
    You should inherit from OcaInteractable instead of MonoBehaviour if
    you wish to attach a runtime controller HUD for the given object. This will enable
    you to display and interact with the parameters of that object when it is selected.

    See OcaControllerHUD for more information.
*/

using System.Collections.Generic;
using UnityEngine;

public class OcaInteractable : MonoBehaviour
{
    // maps class fields to HUD elements
    [HideInInspector]
    public List<HUDElement> HUDElements;
    [HideInInspector]
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