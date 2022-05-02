// :UNyD*>i
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.XR;
using TMPro;


public class InteractionManager : MonoBehaviour
{
    public GameObject right_hand;
    public GameObject left_hand;
    LineRenderer line_renderer_right_hand;
    LineRenderer line_renderer_left_hand;

    Canvas canvas_left_hand;
    TextMeshProUGUI text_display_left_hand;
    Rigidbody hitObjectRigidbody;
    Vector3 prev_left_hand_position;

    FileStream fs;
    StreamWriter sw;

    void Start()
    {
        line_renderer_right_hand = right_hand.GetComponent<LineRenderer>();
        line_renderer_left_hand = left_hand.GetComponent<LineRenderer>();
        canvas_left_hand = left_hand.transform.GetComponentInChildren<Canvas>();
        // text_display_left_hand = canvas_left_hand.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();

        fs = new FileStream("positions.CSV", FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
    }





    enum InteractionStates
    {
        REALEASED,
        HOLDING,
        NONE
    };
    InteractionStates interactionState = InteractionStates.NONE;

    InputDevice deviceRightHand;
    InputDevice deviceLeftHand;
    float debug_draw_distance = 10f;
    float object_hit_distance = 0;
    float object_displacement = 0;
    bool is_already_holding_object = false;
    GameObject hit_object = null;

    [SerializeReference] Vector3 left_hand_position;
    [SerializeReference] Vector3 left_hand_direction;
    [SerializeReference] float min_zoom_factor = 0.01f;
    [SerializeReference] float max_zoom_factor = 1.0f;
    [SerializeReference] float min_hand_object_distance = .10f;


    void FixedUpdate()
    {
        bool is_right_trigger;
        bool is_left_trigger;
        Vector2 left_axis;

        {
            deviceRightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            deviceLeftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            deviceRightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out is_right_trigger);
            deviceLeftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out is_left_trigger);
            deviceLeftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out left_axis);
        }


        left_hand_position = left_hand.transform.position;
        left_hand_direction = left_hand.transform.TransformDirection(Vector3.forward);

        Vector3 right_hand_position = right_hand.transform.position;
        Vector3 right_hand_trace_direction = right_hand.transform.TransformDirection(Vector3.forward);






        ///////////////////////
        // Right hand Raycast
        ///////////////////////


        if (Physics.Raycast(right_hand_position, right_hand_trace_direction, debug_draw_distance) && is_right_trigger)
        {
            line_renderer_right_hand.material.color = Color.red;
        }
        else line_renderer_right_hand.material.color = Color.green;





        /////////////////////
        // Left hand Raycast
        /////////////////////


        RaycastHit leftHitResult;
        if (Physics.Raycast(left_hand_position, left_hand_direction, out leftHitResult, debug_draw_distance) && is_left_trigger)
        {
            if (leftHitResult.transform.gameObject.CompareTag("Interactable") && !hit_object)
            {
                line_renderer_left_hand.startColor = Color.red;
                // text_display_left_hand.text = leftHitResult.transform.gameObject.name;

                Vector3 hit_location = leftHitResult.transform.position;

                hit_object = leftHitResult.transform.gameObject;
                hitObjectRigidbody = hit_object.GetComponent<Rigidbody>();
                object_hit_distance = Vector3.Distance(hit_object.transform.position, left_hand_position);
            }
        }
        else line_renderer_left_hand.startColor = Color.green;


        Debug.DrawRay(left_hand_position, left_hand_direction * debug_draw_distance, Color.red);
        Debug.DrawRay(right_hand_position, right_hand_trace_direction * debug_draw_distance, Color.red);





        ///////////////////////////////////
        // Interaction states management
        ///////////////////////////////////


        bool no_previous_interaction = interactionState == InteractionStates.NONE;
        if (is_left_trigger && no_previous_interaction && hit_object && !(is_already_holding_object))
        {
            interactionState = InteractionStates.HOLDING;
            hit_object.GetComponent<Rigidbody>().isKinematic = true;
            is_already_holding_object = true;
        }
        else if (is_left_trigger == false && interactionState == InteractionStates.HOLDING)
        {
            interactionState = InteractionStates.REALEASED;
            is_already_holding_object = false;
        }





        float zoom = 0;
        switch (interactionState)
        {
            case InteractionStates.HOLDING:
                {
                    float current_distance_to_object = Vector3.Distance(hit_object.transform.position, left_hand_position);

                    if (left_axis.sqrMagnitude > 0)
                    {
                        if ((current_distance_to_object >= min_hand_object_distance) && left_axis.y < 0)
                        {
                            zoom = (left_axis.y * Time.deltaTime * 1.1f);
                        }
                        else if (left_axis.y > 0)
                        {
                            zoom = (left_axis.y * Time.deltaTime * 1.1f);
                        }

                        float zoom_factor = ((current_distance_to_object - min_hand_object_distance) / (.5f - min_hand_object_distance));

                        if (zoom_factor < min_zoom_factor) zoom_factor = min_zoom_factor;
                        else if (zoom_factor > max_zoom_factor) zoom_factor = max_zoom_factor;

                        object_displacement += zoom * zoom_factor * 1.2f;
                    }

                    // hit_object.transform.position = left_hand_position + left_hand_direction * (object_hit_distance + object_displacement);
                    Vector3 newPostion = left_hand_position + left_hand_direction * (object_hit_distance + object_displacement);
                    hitObjectRigidbody.MovePosition(newPostion);
                }
                break;

            case InteractionStates.REALEASED:
                {
                    hitObjectRigidbody.isKinematic = false;
                    hit_object = null;
                    hitObjectRigidbody = null;
                    zoom = 0;
                    interactionState = InteractionStates.NONE;
                    object_displacement = 0;
                    sw.Flush();
                }
                break;
            default:
                break;
        }
    }
}
