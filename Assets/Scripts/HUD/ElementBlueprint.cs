// Used in OcaControllerHUD
// Each HUD element has a name, a prefab and can have an initialization and update functions.
// The purpose of this class is to act as a data container for HUD elements that can be instantiated from this class, hence the 'Blueprint' name.
// Each HUD element must be initialized and for those that are interactable like Sliders, an update function must be provided.
// Hence, each element 'can' implement initialization and update functions to be used by the ControllerHUD system.
// See OcaControllerHUD:142 for an implementation example

using System.Collections.Generic;
using UnityEngine;
using System;


public class ElementBlueprint
{
    public string name;
    public GameObject prefab;

    // todo(adlan): These Action signatures are long and redundant, find a way to make this syntax shorter for readability
    public Action<string, string, OcaInteractable, Dictionary<string, GameObject>,
        Action<string, string, OcaInteractable, Dictionary<string, GameObject>>> initFunction;

    public Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction;

    public ElementBlueprint(
        string name,
        GameObject prefab,
        Action<string, string, OcaInteractable, Dictionary<string, GameObject>,
            Action<string, string, OcaInteractable, Dictionary<string, GameObject>>> initFunction,
        Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction)
    {
        this.name = name;
        this.prefab = prefab;
        this.initFunction = initFunction;
        this.updateFunction = updateFunction;
    }
}