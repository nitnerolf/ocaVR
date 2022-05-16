using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// set RectTransform width/height: RectTransform.sizeDelta

public class ocaHUD : MonoBehaviour
{
    public Vector3 positionOffset;

    string prefabAssetsPath = "HUD/";

    GameObject prefabHUD;
    GameObject prefabLabel;
    GameObject prefabToggle;
    GameObject prefabSlider;

    GameObject HUDInstance;
    TextMeshProUGUI HUD_title;

    // todo(adlan): expose to inspector?
    Transform parent;

    Dictionary<string, GameObject> UIElements;

    [HideInInspector]
    public ocaInteractableBehaviour selection;

    public class ElementBlueprint
    {
        public string name;
        public GameObject prefab;

        // todo(adlan): These Action signatures are long and redundant, find a way to make this syntax shorter for readability
        public Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>>> initFunction;
        public Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction;

        public ElementBlueprint(
            string name,
            GameObject prefab,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>,
                Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>>> initFunction,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
        {
            this.name = name;
            this.prefab = prefab;
            this.initFunction = initFunction;
            this.updateFunction = updateFunction;
        }
    }

    List<ElementBlueprint> elementBlueprints;

    void Start()
    {
        prefabHUD = Resources.Load<GameObject>(prefabAssetsPath + "HUD_Canvas");

        // todo(adlan): We must eventually load these into a hash table
        // effectively avoiding to load these one by one and make the process more streamlined
        // see Resources.LoadAll<>
        prefabLabel = Resources.Load<GameObject>(prefabAssetsPath + "Label");
        prefabToggle = Resources.Load<GameObject>(prefabAssetsPath + "Toggle");
        prefabSlider = Resources.Load<GameObject>(prefabAssetsPath + "Slider");

        HUDInstance = Instantiate<GameObject>(prefabHUD, transform);
        HUDInstance.transform.position += positionOffset;
        HUDInstance.GetComponent<Canvas>().worldCamera = Camera.main;
        parent = HUDInstance.transform.Find("Panel").transform;
        HUD_title = parent.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        HUD_title.text = string.Empty;

        HUDInstance.SetActive(false);

        if (parent == null) Debug.LogError("failed to find Parent");
        if (prefabToggle == null) Debug.LogError("failed to load Toggle prefab");
        if (prefabSlider == null) Debug.LogError("failed to load Slider prefab");

        UIElements = new Dictionary<string, GameObject>();
        selection = null;

        elementBlueprints = new List<ElementBlueprint> {
            {new ElementBlueprint("Label" /*Component Name*/, prefabLabel, null, null)},
            {new ElementBlueprint("Slider" /*Component Name*/, prefabSlider, InitSlider, UpdateSlider)},
            {new ElementBlueprint("Toggle" /*Component Name*/, prefabToggle, InitToggle, UpdateToggle)},
        };
    }

    void Update()
    {

    }

    public void OnSelect()
    {
        Debug.Assert(UIElements.Count == 0, "UIElements.Count is not empty");
        HUDInstance.SetActive(true);

        foreach (var p in selection.guiElementsDescriptor)
        {
            HUD_title.text = selection.transform.name;

            Debug.Assert(!UIElements.ContainsKey(p.fieldName), "Property Name must be unique");
            Debug.Assert(!string.IsNullOrEmpty(p.fieldName), "Property Name must not be an empty string and must be a valid property");

            ElementBlueprint type = elementBlueprints.Find(x => x.name.Contains(p.UIElementType.ToString()));
            GameObject temp = Instantiate<GameObject>(type.prefab, parent);
            UIElements.Add(p.fieldName, temp);

            type.initFunction(p.fieldName, selection, UIElements, type.updateFunction);
        }
    }

    public void OnDeselect()
    {
        UIElements.Clear();
        selection = null;

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            if (parent.transform.GetChild(i).transform.name != "Label")
                Destroy(parent.transform.GetChild(i).gameObject);
        }

        HUD_title.text = "";
        HUDInstance.SetActive(false);
    }






    /////////////////////////////////
    // Static, class scoped functions
    //
    // "pure" functions, meaning they are guaranteed to not alter class instance fields except for the passed arguments
    // preventing unexpected state changes
    // i.e these functions cannot access global class variables and thus are self contained

    static void InitSlider(string fieldName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements, Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
    {
        /*!Important*/ Slider sliderComponent = UIElements[fieldName].GetComponent<Slider>();
        /*!Important*/ sliderComponent.onValueChanged.AddListener(delegate { UpdateSlider(fieldName, selection, UIElements); });

        object o = selection.GetValueByPropertyName(fieldName);
        if (o == null)
            return;

        if (Attribute.IsDefined(selection.GetType().GetField(fieldName), typeof(RangeAttribute)))
        {
            RangeAttribute attr = (RangeAttribute)System.Attribute.GetCustomAttribute(selection.GetType().GetField(fieldName), typeof(RangeAttribute));
            sliderComponent.minValue = attr.min;
            sliderComponent.maxValue = attr.max;
        }
        else if (Attribute.IsDefined(selection.GetType().GetField(fieldName), typeof(MinAttribute)))
        {
            MinAttribute attr = (MinAttribute)System.Attribute.GetCustomAttribute(selection.GetType().GetField(fieldName), typeof(MinAttribute));
            sliderComponent.minValue = attr.min;
        }


        sliderComponent.SetValueWithoutNotify((float)o);
        sliderComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = fieldName + ": " + (float)o;
    }

    static void UpdateSlider(string fieldName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements)
    {
        /*!Important*/ Slider sliderComponent = UIElements[fieldName].GetComponent<Slider>();
        /*!Important*/ selection.GetType().GetField(fieldName).SetValue(selection, (float)sliderComponent.value);
        sliderComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = fieldName + ": " + (float)sliderComponent.value;
    }

    static void InitToggle(string fieldName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements, Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
    {
        /*!Important*/ Toggle toggleComponent = UIElements[fieldName].GetComponent<Toggle>();
        /*!Important*/ toggleComponent.onValueChanged.AddListener(delegate { UpdateToggle(fieldName, selection, UIElements); });

        object o = selection.GetValueByPropertyName(fieldName);
        if (o == null)
            return;

        toggleComponent.isOn = (bool)o;
        toggleComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = fieldName;
    }

    static void UpdateToggle(string fieldName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements)
    {
        Toggle toggleComponent = UIElements[fieldName].GetComponent<Toggle>();
        selection.GetType().GetField(fieldName).SetValue(selection, (bool)toggleComponent.isOn);
    }
}