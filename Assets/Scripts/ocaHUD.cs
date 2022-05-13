using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// set RectTransform width/height: RectTransform.sizeDelta

public class ocaHUD : MonoBehaviour
{
    GameObject prefabHUD;
    GameObject prefabLabel;
    GameObject prefabToggle;
    GameObject prefabSlider;

    GameObject HUDInstance;
    TextMeshProUGUI label;

    Transform parent;

    Dictionary<string, GameObject> UIElements;

    public ocaInteractableBehaviour selection;

    public class TestTypes
    {
        public string componentName;
        public GameObject prefab;

        // todo(adlan): These Action signatures are long and redundant, find a way to make this syntax shorter for readability
        public Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>>> initFunction;
        public Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction;

        public TestTypes(
            string componentName,
            GameObject prefab,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>,
                Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>>> initFunction,
            Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
        {
            this.componentName = componentName;
            this.prefab = prefab;
            this.initFunction = initFunction;
        }
    }

    List<TestTypes> testTypes;

    void Start()
    {
        prefabHUD = Resources.Load<GameObject>("HUD");

        // todo(adlan): We must eventually load these into a hash table
        // effectively avoiding to load these one by one and make the process more streamlined
        // see Resources.LoadAll<>
        prefabLabel = Resources.Load<GameObject>("Label");
        prefabToggle = Resources.Load<GameObject>("Toggle");
        prefabSlider = Resources.Load<GameObject>("Slider");

        HUDInstance = Instantiate<GameObject>(prefabHUD, transform);
        HUDInstance.GetComponent<Canvas>().worldCamera = Camera.main;
        parent = HUDInstance.transform.Find("Panel").transform;
        label = parent.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        label.text = "";

        if (parent == null) print("failed to find Parent");
        if (prefabToggle == null) print("failed to load Toggle prefab");
        if (prefabSlider == null) print("failed to load Slider prefab");

        UIElements = new Dictionary<string, GameObject>();
        selection = null;

        testTypes = new List<TestTypes> {
            {new TestTypes("Slider" /*Component Name*/, prefabSlider, InitSlider, UpdateSlider)},
            {new TestTypes("Toggle" /*Component Name*/, prefabToggle, InitToggle, UpdateToggle)},
        };
    }

    void Update()
    {

    }

    public void OnSelect()
    {
        Debug.Assert(UIElements.Count == 0, "UIElements.Count is not empty");

        foreach (var p in selection.UIParamaterMap)
        {
            label.text = selection.transform.name;

            Debug.Assert(!UIElements.ContainsKey(p.propertyName), "Property Name must be unique");
            Debug.Assert(!string.IsNullOrEmpty(p.propertyName), "Property Name must not be an empty string and must be a valid property");

            TestTypes type = testTypes.Find(x => x.componentName.Contains(p.UIElement.ToString()));
            GameObject temp = Instantiate<GameObject>(type.prefab, parent);
            UIElements.Add(p.propertyName, temp);
            type.initFunction(p.propertyName, selection, UIElements, type.updateFunction);
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

        label.text = "";
    }






    /////////////////////////////////
    // Static, class scoped functions
    //
    // "pure" functions, meaning they are guaranteed to not alter class instance fields except for the passed arguments
    // preventing unexpected state changes
    // i.e these functions cannot access global class variables and thus are self contained

    static void InitSlider(string propertyName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements, Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
    {
        print("------------BEGIN------------");
        Slider sliderComponent = UIElements[propertyName].GetComponent<Slider>();
        object o = selection.GetValueByPropertyName(propertyName);
        if (o == null)
            return;

        sliderComponent.SetValueWithoutNotify(9230);
        sliderComponent.value = 3000;
        print("o: " + propertyName + ":" + (float)o);
        print("sliderComponent.value: " + propertyName + ":" + sliderComponent.value);
        sliderComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = propertyName;


        if (Attribute.IsDefined(selection.GetType().GetField(propertyName), typeof(RangeAttribute)))
        {
            RangeAttribute attr = (RangeAttribute)System.Attribute.GetCustomAttribute(selection.GetType().GetField(propertyName), typeof(RangeAttribute));
            sliderComponent.minValue = attr.min;
            sliderComponent.maxValue = attr.max;

            print(propertyName + ": [RangeAttribute]:" + "(" + attr.min + ", " + attr.max + ")");
            print(propertyName + ": [ComponentVal's]:" + "(" + sliderComponent.minValue + ", " + sliderComponent.maxValue + ")");
            print("value: " + sliderComponent.value);

        }
        else if (Attribute.IsDefined(selection.GetType().GetField(propertyName), typeof(MinAttribute)))
        {
            MinAttribute attr = (MinAttribute)System.Attribute.GetCustomAttribute(selection.GetType().GetField(propertyName), typeof(MinAttribute));
            print("has min attr: " + attr.min);
            sliderComponent.minValue = attr.min;
        }
        sliderComponent.onValueChanged.AddListener(delegate { UpdateSlider(propertyName, selection, UIElements); });
        print("------------END------------");
    }

    static void UpdateSlider(string propertyName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements)
    {
        selection.GetType().GetField(propertyName).SetValue(selection, (float)UIElements[propertyName].GetComponent<Slider>().value);

    }

    static void InitToggle(string propertyName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements, Action<string, ocaInteractableBehaviour, Dictionary<string, GameObject>> updateFunction)
    {
        Toggle toggleComponent = UIElements[propertyName].GetComponent<Toggle>();
        object o = selection.GetValueByPropertyName(propertyName);
        if (o == null)
            return;

        toggleComponent.isOn = (bool)o;
        toggleComponent.transform.GetComponentInChildren<TextMeshProUGUI>().text = propertyName;
    }

    static void UpdateToggle(string propertyName, ocaInteractableBehaviour selection, Dictionary<string, GameObject> UIElements)
    {

    }
}