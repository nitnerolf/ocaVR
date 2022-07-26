using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestParams : OcaInteractable
{
    [Range(.1f, 3f)] public float param1;
    float param2;
    float param3;
    // Start is called before the first frame update
    void Start()
    {
        this.HUDElements.Add(new HUDElement(ElementType.Slider, "param1", "SomeFloat"));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
