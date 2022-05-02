// :UNyD*>i
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.XR;
using TMPro;

// todo:
// hitObject lookat(controller)
// move along Z with A/B
// rotate with joystick
//
public class RayInteractor : MonoBehaviour
{

    LineRenderer lineRenderer;
    Canvas canvas;
    TextMeshProUGUI textDisplay;
    Rigidbody hitObjectRigidbody;

    FileStream fs;
    StreamWriter sw;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // canvas = left_hand.transform.GetComponentInChildren<Canvas>();
        // textDisplay = canvas.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();
    }

    enum InteractionStates
    {
        REALEASED,
        HOLDING,
        NONE
    };
    InteractionStates interactionState = InteractionStates.NONE;

    InputDevice deviceControls;
    float drawDistance = 100f;
    float objectHitDistance = 0;
    float objectDisplacement = 0;
    bool isAlreadyHoldingObject = false;
    GameObject hitObject = null;
    bool collided = false;
    Vector3 rotation;

    public GameObject attachPoint;
    public Vector3 currentPosition;
    public Vector3 currentDirection;
    public float minZoomFactor = 0.01f;
    public float maxZoomFactor = 1.0f;
    public float minDistance = .10f;
    public ForceMode forceMode;
    public float velFactor;
    public float rotationSpeed;


    bool indexTrigger;
    bool gripButton;
    bool primaryButton;
    bool secondaryButton;

    Vector2 axis;
    Vector3 controllerVelocity;
    Vector3 controllerAccel;

    void Update()
    {


        currentPosition = transform.position;
        currentDirection = transform.TransformDirection(Vector3.forward);

        if (tag.Equals("LeftHand"))
            deviceControls = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        else if (tag.Equals("RightHand"))
            deviceControls = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out indexTrigger);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out gripButton);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out primaryButton);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out secondaryButton);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out axis);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out controllerVelocity);
        deviceControls.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAcceleration, out controllerAccel);
    }

    void FixedUpdate()
    {


        /////////////////////
        // Left hand Raycast
        /////////////////////


        RaycastHit hitResult;
        if (Physics.Raycast(currentPosition, currentDirection, out hitResult, drawDistance) && (indexTrigger | gripButton))
        {
            if (hitResult.transform.gameObject.CompareTag("Interactable") && !hitObject)
            {
                lineRenderer.startColor = Color.red;
                // textDisplay.text = hitResult.transform.gameObject.name;

                Vector3 hit_location = hitResult.transform.position;

                hitObject = hitResult.transform.gameObject;
                hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                hitObjectRigidbody.isKinematic = false;
                // hitObjectRigidbody.detectCollisions = false;
                hitObjectRigidbody.useGravity = false;
                rotation = hitObject.transform.rotation.eulerAngles;
                // rotation = Vector3.zero;
                objectHitDistance = Vector3.Distance(hitObject.transform.position, currentPosition);
                // hitObject.transform.parent = attachPoint.transform;
                if (gripButton)
                    hitObject.transform.position = transform.position;
            }
        }
        else lineRenderer.startColor = Color.green;
        Debug.DrawRay(currentPosition, currentDirection * drawDistance, Color.red);




        ///////////////////////////////////
        // Interaction states management
        ///////////////////////////////////


        bool noPreviousInteracion = interactionState == InteractionStates.NONE;
        if ((indexTrigger | gripButton) && noPreviousInteracion && hitObject && !(isAlreadyHoldingObject))
        {
            interactionState = InteractionStates.HOLDING;
            // hitObject.GetComponent<Rigidbody>().isKinematic = true;
            isAlreadyHoldingObject = true;
        }
        else if ((indexTrigger | gripButton) == false && interactionState == InteractionStates.HOLDING)
        {
            interactionState = InteractionStates.REALEASED;
            isAlreadyHoldingObject = false;
        }





        float zoom = 0;
        switch (interactionState)
        {
            case InteractionStates.HOLDING:
                {
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, currentPosition);
                    if (indexTrigger)
                    {
                        if (primaryButton || secondaryButton)
                        {
                            if ((currentDistanceToObject >= minDistance) && primaryButton)
                            {
                                zoom = (-1 * Time.deltaTime * 1.1f);
                            }
                            else if (secondaryButton)
                            {
                                zoom = (1 * Time.deltaTime * 1.1f);
                            }

                            float zoom_factor = ((currentDistanceToObject - minDistance) / (.5f - minDistance));

                            if (zoom_factor < minZoomFactor) zoom_factor = minZoomFactor;
                            else if (zoom_factor > maxZoomFactor) zoom_factor = maxZoomFactor;

                            if (collided && primaryButton)
                                zoom = 0;

                            objectDisplacement += zoom * zoom_factor * 1.2f;
                        }

                        Vector3 newPostion = currentPosition + currentDirection * (objectHitDistance + objectDisplacement);

                        Vector3 _rotation = Vector3.zero;
                        if (Mathf.Abs(axis.x) > .7f)
                            _rotation.y = axis.x * -1f;
                        if (Mathf.Abs(axis.y) > .7f)
                            _rotation.x = axis.y;
                        // rotation += _rotation * rotationSpeed * Time.deltaTime;
                        // rotation += new Vector3(axis.y, axis.x);
                        // transform.rotation * Quaternion.Euler(rotation) * Quaternion.Euler(Vector3.forward) *
                        // hitObjectRigidbody.MoveRotation(hitObject.transform.localToWorldMatrix.rotation * Quaternion.Euler(new Vector3(axis.x, axis.y)));
                        // hitObjectRigidbody.MoveRotation(hitObject.transform.localToWorldMatrix.rotation);
                        // hitObject.transform.Rotate(new Vector3(_rotation.x, _rotation.y) * rotationSpeed * Time.deltaTime, Space.World);
                        Vector3 right = Vector3.Cross(Vector3.up, transform.forward);
                        // var xRot = Quaternion.AngleAxis(rotation.x * rotationSpeed * Time.deltaTime, right);
                        // var zRot = Quaternion.AngleAxis(rotation.y * rotationSpeed * Time.deltaTime, xRot * transform.forward);

                        // hitObject.transform.rotation = Quaternion.Euler(rotation);
                        // hitObject.transform.Rotate(Vector3.left * axis.y, Space.World);
                        // hitObjectRigidbody.MoveRotation(GetComponent<Rigidbody>().rotation * Quaternion.Euler(rotation));
                        var xRot = Quaternion.AngleAxis((rotation.x * -1) * rotationSpeed * Time.deltaTime, Vector3.left);
                        var yRot = Quaternion.AngleAxis(rotation.y * rotationSpeed * Time.deltaTime, Vector3.forward);
                        hitObjectRigidbody.rotation = Quaternion.Euler(rotation) * GetComponent<Rigidbody>().rotation * (yRot);
                        // hitObjectRigidbody.rotation = GetComponent<Rigidbody>().rotation * Quaternion.AngleAxis(rotation.y * rotationSpeed * Time.deltaTime, Vector3.forward);
                        // hitObjectRigidbody.rotation = GetComponent<Rigidbody>().rotation * Quaternion.AngleAxis((rotation.x *-1)* rotationSpeed * Time.deltaTime, Vector3.left);
                        hitObjectRigidbody.MovePosition(newPostion);
                    }
                    else if (gripButton)
                    {
                        // hitObjectRigidbody.MoveRotation(Quaternion.Euler(axis));
                        Vector3 _rotation = Vector3.zero;
                        if (Mathf.Abs(axis.x) > .7f)
                            _rotation.y = axis.x * -1f;
                        if (Mathf.Abs(axis.y) > .7f)
                            _rotation.x = axis.y;
                        rotation += _rotation * rotationSpeed;

                        hitObjectRigidbody.MoveRotation(Quaternion.Euler(rotation));
                        hitObjectRigidbody.MovePosition(transform.position);
                    }
                }
                break;

            case InteractionStates.REALEASED:
                {
                    attachPoint.transform.DetachChildren();
                    // hitObject.transform.parent = null;
                    rotation = Vector3.zero;
                    hitObjectRigidbody.isKinematic = false;
                    hitObjectRigidbody.useGravity = true;
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, currentPosition);
                    hitObjectRigidbody.velocity = controllerVelocity * Mathf.Clamp(currentDistanceToObject, 2f, 8f);
                    hitObject = null;
                    hitObjectRigidbody = null;
                    zoom = 0;
                    interactionState = InteractionStates.NONE;
                    objectDisplacement = 0;
                    // sw.Flush();
                }
                break;
            default:
                break;
        }
    }

    void OnCollisionStay(Collision col)
    {
        if (col.gameObject.tag.Equals("Interactable"))
            collided = true;
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.tag.Equals("Interactable"))
            collided = false;
    }
}
