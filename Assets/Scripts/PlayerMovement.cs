using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Start()
    {
        Debug.Log("PlayerMovement: Initialised");
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        Vector3 newPosition = transform.position + move * moveSpeed * Time.deltaTime;
        newPosition.y = transform.position.y;

        transform.position = newPosition;
    }
}
