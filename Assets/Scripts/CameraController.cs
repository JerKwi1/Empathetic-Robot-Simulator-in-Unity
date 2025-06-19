using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 30f;
    public float panAcceleration = 30f;
    public float panDeceleration = 30f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 150f;

    private Vector3 currentVelocity = Vector3.zero;
    private bool isPanning = false;
    private float yaw = 0f;
    private float pitch = 0f;

    // Initializes yaw and pitch based on the camera’s current orientation.
    void Start()
    {

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    // Checks for middle‐mouse button input to toggle panning/rotation mode,
    // updates camera orientation when dragging, and applies smooth panning movement.
    void Update()
    {

        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isPanning)
        {
    
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw   += mx * rotationSpeed * Time.deltaTime;
            pitch -= my * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v).normalized;
            Vector3 worldDir = transform.TransformDirection(inputDir);
            Vector3 targetVel = worldDir * panSpeed;
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVel,
                panAcceleration * Time.deltaTime
            );
        }
        else
        {
    
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                panDeceleration * Time.deltaTime
            );
        }


        transform.position += currentVelocity * Time.deltaTime;
    }
}