using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 30f;            // Faster pan speed
    public float panAcceleration = 30f;     // Quicker acceleration
    public float panDeceleration = 30f;     // Quicker deceleration

    [Header("Rotation Settings")]
    public float rotationSpeed = 150f;      // Degrees per second when rotating

    // Internal state
    private Vector3 currentVelocity = Vector3.zero;
    private bool isPanning = false;
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        // Initialize yaw/pitch to current camera orientation
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // Middle mouse button pressed/released
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
            // Rotation
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw   += mx * rotationSpeed * Time.deltaTime;
            pitch -= my * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            // ─── Panning ───
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
            // Decelerate when not panning
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                panDeceleration * Time.deltaTime
            );
        }

        // Apply movement
        transform.position += currentVelocity * Time.deltaTime;
    }
}