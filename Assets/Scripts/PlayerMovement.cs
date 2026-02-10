using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Start()
    {
        // Log movement setup
        Debug.Log("PlayerMovement: Initialised");
    }

    void Update()
    {
        // Read movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Build move direction
        Vector3 move = transform.right * x + transform.forward * z;

        // Calculate new position
        Vector3 newPosition = transform.position + move * moveSpeed * Time.deltaTime;

        // Keep constant height
        newPosition.y = transform.position.y;

        // Apply world position
        transform.position = newPosition;
    }
}
