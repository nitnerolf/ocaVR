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


    enum InteractionStates
    {
        START,
        REALEASED,
        HOLDING,
        HOVERING,
        END,
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
    Vector3 hitLocation;
    Vector3 initialRotation;
    Vector3 handPosition;
    Vector3 handDirection;
    Quaternion initialRotationQ;
    float hitObjectCenterToImpactDistance;
    float zoom = 0;

    // todo(ad): check if these are still revelant
    public GameObject attachPoint;
    public LayerMask blockingMask;
    [Space]
    public float minZoomFactor = 0.01f;
    public float maxZoomFactor = 1.0f;
    public float minDistance = .10f;
    public AnimationCurve zoomFactorAttenuation;
    [Space]
    public float rotationSpeed;

    bool indexTrigger;
    bool gripButton;
    bool primaryButton;
    bool secondaryButton;

    Vector2 axis;
    Vector3 controllerVelocity;
    Vector3 controllerAccel;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // canvas = left_hand.transform.GetComponentInChildren<Canvas>();
        // textDisplay = canvas.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();

    }

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

        if (hit)
        {
            lineRenderer.SetPosition(1, Vector3.forward * hitResult.distance);
            lineRenderer.startColor = Color.white;
        }
        else
        {
            lineRenderer.SetPosition(1, Vector3.forward * drawDistance);
        }

        if (hit && (indexTrigger | gripButton))
        {
            if (hitResult.transform.gameObject.CompareTag("Interactable") && !hitObject)
            {
                lineRenderer.startColor = Color.red;
                // textDisplay.text = hitResult.transform.gameObject.name;

                Vector3 hitLocationAtCenter = hitResult.transform.position;
                objectHitDistanceAtCenter = Vector3.Distance(handPosition, hitLocationAtCenter);

                hitObject = hitResult.transform.gameObject;
                hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                hitObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                hitObjectRigidbody.isKinematic = true;
                hitObjectRigidbody.useGravity = false;
                initialRotation = hitObject.transform.rotation.eulerAngles;
                initialRotationQ = hitObjectRigidbody.rotation;
                // hitObjectCenterToImpactDistance = objectHitDistanceAtCenter - hitResult.distance;
                hitObjectCenterToImpactDistance = Vector3.Distance(hitLocationAtCenter, hitResult.point);

                if (gripButton)
                {
                    hitObject.transform.position = handPosition + handDirection * (hitObjectCenterToImpactDistance * 1.3f);
                    // hitObjectRigidbody.MovePosition(handPosition + handDirection * (hitObjectCenterToImpactDistance ));
                }
                if (hitObjectRigidbody.constraints != (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ))
                    hitObject.transform.parent = attachPoint.transform;
            }
        }
        else
        {
            lineRenderer.startColor = Color.green;
        }



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
                    interactionState = InteractionStates.NONE;
                    objectDisplacement = 0;
                }
                break;
            default: break;
        }
    }

    void FixedUpdate()
    {
        switch (interactionState)
        {
            case InteractionStates.HOLDING:
                {
                    float currentDistanceToObject = Vector3.Distance(hitObject.transform.position, handPosition) - hitObjectCenterToImpactDistance;
                    if (indexTrigger && !gripButton)
                    {
                        if (primaryButton || secondaryButton)
                        {
                            if (((currentDistanceToObject) >= minDistance) && primaryButton)
                            {
                                zoom = (-1 * Time.deltaTime);
                            }
                            else if (secondaryButton)
                            {
                                zoom = (1 * Time.deltaTime);
                            }

                            float zoom_factor = ((currentDistanceToObject - minDistance) / (minDistance));
                            if (zoom_factor < minZoomFactor) zoom_factor = minZoomFactor;

                            if (zoom_factor > maxZoomFactor) zoom_factor = maxZoomFactor;

                            if (collided && primaryButton)
                                zoom = 0;

                            objectDisplacement += zoom * zoom_factor * zoomFactorAttenuation.Evaluate(zoom_factor / maxZoomFactor);
                        }

                        Vector3 newPostion = handPosition + handDirection * (objectHitDistanceAtCenter + objectDisplacement);
                        hitObjectRigidbody.MovePosition(newPostion);
                    }
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
