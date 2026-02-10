using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 120f;

    private float xRotation = 0f;

    void Awake()
    {
        Debug.Log("PlayerLook: Awake");
    }

    void OnEnable()
    {
        Debug.Log("PlayerLook: Enabled");
    }

    void OnDisable()
    {
        Debug.Log("PlayerLook: Disabled");
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    public void AddRecoil(float pitchUpDegrees, float yawDegrees)
    {
        xRotation -= pitchUpDegrees;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * yawDegrees);
        }

        Debug.Log("PlayerLook: Recoil added pitch " + pitchUpDegrees + " yaw " + yawDegrees);
    }
}
