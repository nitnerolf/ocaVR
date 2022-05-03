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
    Vector3 initialRotation;
    Quaternion initialRotationQ;
    Quaternion initialRotationQController;

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
                // hitObjectRigidbody.detectCollisions = false;
                hitObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                hitObjectRigidbody.isKinematic = true;
                hitObjectRigidbody.useGravity = false;
                initialRotation = hitObject.transform.rotation.eulerAngles;
                initialRotationQ = hitObjectRigidbody.rotation;
                initialRotationQController = GetComponent<Rigidbody>().rotation;
                // initialRotation = Vector3.zero;
                objectHitDistance = Vector3.Distance(hitObject.transform.position, currentPosition);
                // hitObject.transform.parent = attachPoint.transform;
                if (gripButton)
                    hitObject.transform.position = transform.position;
            }
        }
        else lineRenderer.startColor = Color.green;
        Debug.DrawRay(currentPosition, currentDirection * drawDistance, Color.red);


        switch (interactionState)
        {
            case InteractionStates.HOLDING:
                {
                    if (indexTrigger)
                    {
                        if (hitObjectRigidbody.constraints == (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ))
                            break;

                        Vector3 _rotation = Vector3.zero;
                        if (Mathf.Abs(axis.x) > .7f)
                        {
                            _rotation.y = axis.x * -1f;
                            hitObject.transform.Rotate(new Vector3(0, -1, 0) * axis.x * rotationSpeed * Time.fixedDeltaTime, Space.World);
                        }
                        if (Mathf.Abs(axis.y) > .7f)
                        {
                            _rotation.x = axis.y;
                            hitObject.transform.Rotate(new Vector3(1, 0, 0) * axis.y * rotationSpeed * Time.fixedDeltaTime, Space.World);
                        }
                        initialRotation += _rotation * rotationSpeed * Time.deltaTime;
                    }
                }
                break;

            default: break;
        }
    }

    void FixedUpdate()
    {


        /////////////////////
        // Left hand Raycast
        /////////////////////




        ///////////////////////////////////
        // Interaction states management
        ///////////////////////////////////


        bool noPreviousInteracion = interactionState == InteractionStates.NONE;
        if ((indexTrigger | gripButton) && noPreviousInteracion && hitObject && !(isAlreadyHoldingObject))
        {
            interactionState = InteractionStates.HOLDING;
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
                        hitObjectRigidbody.MovePosition(newPostion);
                    }
                    else if (gripButton)
                    {
                        if (hitObjectRigidbody.constraints != (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ))
                            hitObjectRigidbody.rotation = (GetComponent<Rigidbody>().rotation * Quaternion.Euler(initialRotation));
                        hitObjectRigidbody.MovePosition(transform.position);
                    }
                }
                break;

            case InteractionStates.REALEASED:
                {
                    attachPoint.transform.DetachChildren();
                    initialRotation = Vector3.zero;
                    hitObjectRigidbody.isKinematic = false;
                    hitObjectRigidbody.useGravity = true;
                    hitObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, currentPosition);
                    hitObjectRigidbody.velocity = controllerVelocity * Mathf.Clamp(currentDistanceToObject, 2f, 8f);
                    hitObject = null;
                    hitObjectRigidbody = null;
                    zoom = 0;
                    interactionState = InteractionStates.NONE;
                    objectDisplacement = 0;
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
