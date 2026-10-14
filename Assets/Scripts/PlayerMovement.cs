using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Esquema de control de cada jugador (1v1 en el mismo teclado)
    public enum ControlScheme
    {
        InputActions, // PlayerInput + OnMove (comportamiento original)
        WASD,         // Jugador 1
        Flechas       // Jugador 2
    }

    public float speed = 5f;

    [Header("Controles")]
    public ControlScheme controls = ControlScheme.InputActions;

    private Vector2 movementInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Con teclas fijas no usamos PlayerInput, asi los dos
        // jugadores no se pelean por el mismo teclado
        if (controls != ControlScheme.InputActions)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
                playerInput.enabled = false;
        }
    }

    void OnMove(InputValue value)
    {
        if (controls != ControlScheme.InputActions)
            return;

        // Direccion que quiere moverse
        movementInput = value.Get<Vector2>();
    }

    void Update()
    {
        if (controls == ControlScheme.InputActions)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        Vector2 direction = Vector2.zero;

        if (controls == ControlScheme.WASD)
        {
            if (keyboard.wKey.isPressed) direction.y += 1;
            if (keyboard.sKey.isPressed) direction.y -= 1;
            if (keyboard.dKey.isPressed) direction.x += 1;
            if (keyboard.aKey.isPressed) direction.x -= 1;
        }
        else
        {
            if (keyboard.upArrowKey.isPressed) direction.y += 1;
            if (keyboard.downArrowKey.isPressed) direction.y -= 1;
            if (keyboard.rightArrowKey.isPressed) direction.x += 1;
            if (keyboard.leftArrowKey.isPressed) direction.x -= 1;
        }

        // Misma velocidad en diagonal
        movementInput = Vector2.ClampMagnitude(direction, 1f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movementInput*speed;
    }
}
