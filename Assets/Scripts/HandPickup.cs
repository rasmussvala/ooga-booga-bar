using UnityEngine;
using UnityEngine.InputSystem;

public class HandPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;     // POVCamera
    [SerializeField] private Transform rightHand;     // RightHand anchor

    [Header("Settings")]
    [SerializeField] private float reachDistance = 2.5f;
    [SerializeField] private LayerMask pickupMask;
    [SerializeField] private float holdSmoothing = 20f;
    [SerializeField] private float throwForce = 6f;

    private InputAction interactAction;
    private InputAction throwAction;

    private Rigidbody heldBody;
    private Collider[] heldColliders;
    private PickupHighlight currentHighlight;

    private void Awake()
    {
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Mouse>/rightButton");
        throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        interactAction.Enable();
        throwAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
        throwAction.Disable();
    }

    private void Update()
    {
        UpdateHighlight();

        if (interactAction.WasPressedThisFrame())
        {
            if (heldBody == null) TryPickUp();
            else Drop();
        }

        if (heldBody != null && throwAction.WasPressedThisFrame())
        {
            Throw();
        }
    }

    private void FixedUpdate()
    {
        if (heldBody == null) return;

        // Smoothly move the held object to the hand
        Vector3 targetPos = rightHand.position;
        Quaternion targetRot = rightHand.rotation;

        heldBody.MovePosition(Vector3.Lerp(heldBody.position, targetPos, holdSmoothing * Time.fixedDeltaTime));
        heldBody.MoveRotation(Quaternion.Slerp(heldBody.rotation, targetRot, holdSmoothing * Time.fixedDeltaTime));
    }

    private void TryPickUp()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, pickupMask))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null) return;

            heldBody = rb;
            heldBody.useGravity = false;
            heldBody.isKinematic = true;   // we drive it manually while held
            heldBody.interpolation = RigidbodyInterpolation.Interpolate;

            // Don't let the bottle push the player around
            heldColliders = heldBody.GetComponentsInChildren<Collider>();
            var playerCol = GetComponent<Collider>();
            foreach (var c in heldColliders)
                if (playerCol != null) Physics.IgnoreCollision(c, playerCol, true);
        }
    }

    private void Drop()
    {
        Release(Vector3.zero);
    }

    private void Throw()
    {
        Release(playerCamera.transform.forward * throwForce);
    }

    private void Release(Vector3 velocity)
    {
        heldBody.isKinematic = false;
        heldBody.useGravity = true;
        heldBody.linearVelocity = velocity;   // use .velocity on Unity 2022 and older

        var playerCol = GetComponent<Collider>();
        foreach (var c in heldColliders)
            if (playerCol != null) Physics.IgnoreCollision(c, playerCol, false);

        heldBody = null;
        heldColliders = null;
    }

    private void UpdateHighlight()
    {
        PickupHighlight found = null;

        // Only highlight when your hand is empty
        if (heldBody == null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, pickupMask) && hit.rigidbody != null)
                found = hit.rigidbody.GetComponent<PickupHighlight>();
        }

        if (found != currentHighlight)
        {
            if (currentHighlight != null) currentHighlight.SetHighlighted(false);
            if (found != null) found.SetHighlighted(true);
            currentHighlight = found;
        }
    }

}
