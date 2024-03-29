/*
    The purpose of this class is to dynamically generate an interactable HUD interface
    next to the controller it is attached on.

    The interface will be populated with premade UI elements called 'ElementBlueprint'.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class OcaControllerHUD : MonoBehaviour
{
    public Vector3 positionOffset;

    string prefabAssetsPath = "HUD/";

    GameObject prefabHUDCanvas;
    GameObject prefabLabel;
    GameObject prefabToggle;
    GameObject prefabSlider;

    GameObject HUDCanvas;
    TextMeshProUGUI HUDTitle;
    // todo(adlan): expose to inspector?
    Transform parent;

    [HideInInspector]
    public OcaInteractable target;

    List<ElementBlueprint> elementBlueprints;
    Dictionary<string, GameObject> elementInstances;

    void Start()
    {
        prefabHUDCanvas = Resources.Load<GameObject>(prefabAssetsPath + "HUDCanvas");

        /*
            todo(adlan): We should eventually load these into a hash table
            effectively avoiding to load these one by one and make the process more streamlined
            see Resources.LoadAll<>
        */
    
        prefabLabel = Resources.Load<GameObject>(prefabAssetsPath + "Label");
        prefabToggle = Resources.Load<GameObject>(prefabAssetsPath + "Toggle");
        prefabSlider = Resources.Load<GameObject>(prefabAssetsPath + "Slider");

        HUDCanvas = Instantiate<GameObject>(prefabHUDCanvas, transform);
        HUDCanvas.transform.position += positionOffset;
        HUDCanvas.GetComponent<Canvas>().worldCamera = Camera.main;

        /*
            Please be mindful about changing the HUDCanvas hierarchy or any names
        */
        parent = HUDCanvas.transform.Find("Panel").transform;
        HUDTitle = parent.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        HUDTitle.text = string.Empty;

        HUDCanvas.SetActive(false);

        if (parent == null) Debug.LogError("failed to find Parent");
        if (prefabToggle == null) Debug.LogError("failed to load Toggle prefab");
        if (prefabSlider == null) Debug.LogError("failed to load Slider prefab");


        /*
            Contrainer for HUD element prefabs and their functionnality
            from which the actual HUD element will be instantiated
        */
        elementBlueprints = new List<ElementBlueprint> {
            {new ElementBlueprint("Label", prefabLabel, InitLabel, null)},
            {new ElementBlueprint("Slider", prefabSlider, InitSlider, UpdateSlider)},
            {new ElementBlueprint("Toggle", prefabToggle, InitToggle, UpdateToggle)},
        };

        /*
            Container holding the instances of ElementBlueprint
            (actual HUD elements that can be seen on the HUDCanvas)
        */
        elementInstances = new Dictionary<string, GameObject>();

        // Reference to the interactable gameObject
        target = null;
    }

    public void OnSelect()
    {
        if (elementInstances.Count != 0) elementInstances.Clear();
        Debug.Assert(elementInstances.Count == 0, "elementInstances is not empty");
        HUDCanvas.SetActive(true);
        target.InjectHUDReference(this);

        foreach (var p in target.HUDElements)
        {
            HUDTitle.text = target.transform.name;

            Debug.Assert(!elementInstances.ContainsKey(p.fieldName), "Property Name must be unique");
            Debug.Assert(!string.IsNullOrEmpty(p.fieldName), "Property Name must not be an empty string and must be a valid property");

            ElementBlueprint type = elementBlueprints.Find(x => x.name.Contains(p.elementType.ToString()));
            GameObject temp = Instantiate<GameObject>(type.prefab, parent);
            elementInstances.Add(p.fieldName, temp);

            type.initFunction(p.fieldName, p.displayName, target, elementInstances, type.updateFunction);
        }
    }

    public void OnDeselect()
    {
        elementInstances.Clear();
        target = null;
        Transform TitleElement = parent.transform.Find("Title");

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (!child.Equals(TitleElement) && !child.CompareTag("Spacer"))
                Destroy(child.gameObject);
        }

        HUDTitle.text = string.Empty;
        HUDCanvas.SetActive(false);
    }






    /////////////////////////////////
    // Static, class scoped functions
    //
    // "pure" functions, meaning they are guaranteed to not alter class instance fields except for the passed arguments
    // preventing unexpected state changes
    // i.e these functions cannot access global class variables and thus are self contained

    // 'elementInstances' holds references to the actual UI elements that were instanciated for the given properties.
    // The idea is to get access to the elementInstances[<key>].<value> and set the relevent data for the UI component you're working with

    private void InitLabel(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances, Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction = null)
    {
        string textToDisplay = String.IsNullOrEmpty(displayName) ? fieldName : displayName;
        elementInstances[fieldName].GetComponent<TextMeshProUGUI>().text = textToDisplay + ": " + (string)target.GetValueByFieldName(fieldName).ToString();
        // target.GetFieldByName(fieldName).

    }

    private void UpdateLabel(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances, Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction = null)
    {
        string textToDisplay = String.IsNullOrEmpty(displayName) ? fieldName : displayName;
        elementInstances[fieldName].GetComponent<TextMeshProUGUI>().text = textToDisplay + ": " + (string)target.GetValueByFieldName(fieldName).ToString();
    }

    static void InitSlider(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances, Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction)
    {
        Slider sliderComponent = elementInstances[fieldName].GetComponent<Slider>();
        sliderComponent.onValueChanged.AddListener(delegate { UpdateSlider(fieldName, displayName, target, elementInstances); });

        object o = target.GetValueByFieldName(fieldName);
        if (o == null)
            return;

        if (Attribute.IsDefined(target.GetType().GetField(fieldName), typeof(RangeAttribute)))
        {
            RangeAttribute attr = (RangeAttribute)System.Attribute.GetCustomAttribute(target.GetType().GetField(fieldName), typeof(RangeAttribute));
            sliderComponent.minValue = attr.min;
            sliderComponent.maxValue = attr.max;
        }
        else if (Attribute.IsDefined(target.GetType().GetField(fieldName), typeof(MinAttribute)))
        {
            MinAttribute attr = (MinAttribute)System.Attribute.GetCustomAttribute(target.GetType().GetField(fieldName), typeof(MinAttribute));
            sliderComponent.minValue = attr.min;
        }

        sliderComponent.value = ((float)o);

        string textToDisplay = String.IsNullOrEmpty(displayName) ? fieldName : displayName;
        sliderComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = textToDisplay + ": " + (float)o;
    }

    static void UpdateSlider(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances)
    {
        /*!Important*/
        Slider sliderComponent = elementInstances[fieldName].GetComponent<Slider>();
        /*!Important*/
        target.GetType().GetField(fieldName).SetValue(target, (float)sliderComponent.value);
        sliderComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = fieldName + ": " + (float)sliderComponent.value;
    }

    static void InitToggle(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances, Action<string, string, OcaInteractable, Dictionary<string, GameObject>> updateFunction)
    {
        Toggle toggleComponent = elementInstances[fieldName].GetComponent<Toggle>();
        toggleComponent.onValueChanged.AddListener(delegate { UpdateToggle(fieldName, displayName, target, elementInstances); });

        object o = target.GetValueByFieldName(fieldName);
        if (o == null)
            return;

        toggleComponent.isOn = (bool)o;
        string textToDisplay = String.IsNullOrEmpty(displayName) ? fieldName : displayName;
        toggleComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = textToDisplay;
    }

    static void UpdateToggle(string fieldName, string displayName, OcaInteractable target, Dictionary<string, GameObject> elementInstances)
    {
        Toggle toggleComponent = elementInstances[fieldName].GetComponent<Toggle>();
        target.GetType().GetField(fieldName).SetValue(target, (bool)toggleComponent.isOn);
    }
}