using UnityEngine;

public class CameraController : MonoBehaviour {
    public float moveSpeed = 10f; // Speed of movement
    public float lookSpeed = 100f; // Speed of mouse look

    private float yaw = 0f;
    private float pitch = 0f;

    void Update() {
        // Check if the right mouse button is held down for rotation
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            yaw += Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -90f, 90f); // Restrict pitch angle

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // Camera movement using WASD/arrow keys
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float vertical = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow

        Vector3 movement = transform.right * horizontal + transform.forward * vertical;
        transform.position += movement * moveSpeed * Time.deltaTime;

        // Optional: Move up and down using Q and E
        if (Input.GetKey(KeyCode.Q)) {
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E)) {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
    }//
}
