// :UNyD*>i
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    float objectHitDistanceAtCenter = 0;
    float objectDisplacement = 0;
    bool isAlreadyHoldingObject = false;
    GameObject hitObject = null;
    bool collided = false;
    Vector3 initialRotation;
    Vector3 handPosition;
    Vector3 handDirection;
    Quaternion initialRotationQ;
    float hitObjectCenterToImpactDistance;

    // todo(ad): check if these are still revelant
    float minZoomFactor = 0.01f;
    float maxZoomFactor = 1.0f;
    float minDistance = .20f;

    public GameObject attachPoint;
    public float distanceFactor;
    public float rotationSpeed;
    public LayerMask blockingMask;

    bool indexTrigger;
    bool gripButton;
    bool primaryButton;
    bool secondaryButton;

    Vector2 axis;
    Vector3 controllerVelocity;
    Vector3 controllerAccel;

    void Update()
    {
        handPosition = transform.position;
        handDirection = transform.TransformDirection(Vector3.forward);

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
        bool hit = Physics.Raycast(handPosition, handDirection, out hitResult, drawDistance, blockingMask);

        if (hit && (indexTrigger | gripButton))
        {
            if (hitResult.transform.gameObject.CompareTag("Interactable") && !hitObject)
            {
                lineRenderer.startColor = Color.red;
                // textDisplay.text = hitResult.transform.gameObject.name;

                Vector3 hitLocationAtCenter = hitResult.transform.position;

                hitObject = hitResult.transform.gameObject;
                hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                hitObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                hitObjectRigidbody.isKinematic = true;
                hitObjectRigidbody.useGravity = false;
                initialRotation = hitObject.transform.rotation.eulerAngles;
                initialRotationQ = hitObjectRigidbody.rotation;
                objectHitDistanceAtCenter = Vector3.Distance(handPosition, hitLocationAtCenter);
                hitObjectCenterToImpactDistance = objectHitDistanceAtCenter - hitResult.distance;

                if (gripButton)
                {
                    hitObject.transform.position = handPosition + handDirection * (hitObjectCenterToImpactDistance * 1.3f);
                    // hitObjectRigidbody.MovePosition(handPosition + handDirection * (hitObjectCenterToImpactDistance ));
                }
                if (hitObjectRigidbody.constraints != (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ) && !indexTrigger)
                    hitObject.transform.parent = attachPoint.transform;
            }
        }
        else
        {
            lineRenderer.startColor = Color.green;
        }

        switch (interactionState)
        {
            case InteractionStates.HOLDING:
                {
                    if (indexTrigger)
                    {
                        if (hitObjectRigidbody.constraints == (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ))
                            break;

                        if (Mathf.Abs(axis.x) > .7f)
                        {
                            hitObject.transform.Rotate(transform.forward * -1 * axis.x * rotationSpeed * Time.deltaTime, Space.World);
                        }
                        if (Mathf.Abs(axis.y) > .7f)
                        {
                            hitObject.transform.Rotate(Vector3.Cross(transform.up, transform.forward) * axis.y * rotationSpeed * Time.deltaTime, Space.World);
                        }
                        // initialRotation += _rotation * rotationSpeed * Time.deltaTime;
                    }
                }
                break;

            default: break;
        }
    }

    void FixedUpdate()
    {
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
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, handPosition);
                    if (indexTrigger && !gripButton)
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

                        Vector3 newPostion = handPosition + handDirection * (objectHitDistanceAtCenter + objectDisplacement);
                        hitObjectRigidbody.MovePosition(newPostion);
                    }
                    else if (gripButton && !indexTrigger)
                    {
                        if (hitObjectRigidbody.constraints != (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ))
                        {
                            // hitObject.transform.rotation = transform.rotation;
                            // hitObjectRigidbody.rotation = (Quaternion.Euler(initialRotation));
                        }

                        // hitObjectRigidbody.MovePosition(handPosition + handDirection * (hitObjectCenterToImpactDistance * 1.3f));
                        // hitObjectRigidbody.MovePosition(handPosition + handDirection * hitObject.GetComponent<Collider>().bounds.size.magnitude);
                    }
                }
                break;

            case InteractionStates.REALEASED:
                {
                    attachPoint.transform.DetachChildren();
                    initialRotation = Vector3.zero;
                    hitObjectRigidbody.isKinematic = false;
                    hitObjectRigidbody.useGravity = true;
                    // hitObjectRigidbody.detectCollisions = true;

                    hitObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, handPosition);
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
