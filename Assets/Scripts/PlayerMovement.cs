using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Vector2 movementInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnMove(InputValue value)
    {
        // Direccion que quiere moverse
        movementInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movementInput*speed;
    }
}
