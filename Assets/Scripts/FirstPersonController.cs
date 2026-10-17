using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float gravity = -9.8f;

    [Header("Look")]
    public float mouseSensitivity = 2.0f;
    public float verticalLookLimit = 70.0f;

    [Header("Interaction")]
    public float interactDistance = 3.5f;

    private Transform cameraTransform;
    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        Camera childCam = GetComponentInChildren<Camera>();
        if (childCam != null)
        {
            cameraTransform = childCam.transform;
        }
        else if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (cameraTransform == null) return;

        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleInteraction()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.root == transform) continue;

                SlotInteractable interactable = hit.collider.GetComponentInParent<SlotInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    break;
                }
            }
        }
    }
}