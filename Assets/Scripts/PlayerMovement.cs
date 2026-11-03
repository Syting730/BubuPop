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

    // para saber donde esta el centro de cada casilla
    public Grid grid;

    private Vector2 movementInput;
    private Rigidbody2D rb;

    /* Codigo IA
    public void SetEntradaIA(Vector2 direccion)
    {
        entradaIA = direccion;
    }

    // Para resolver diagonales: manda la ultima tecla pulsada
    private int prevX, prevY;
    private bool ejeHorizontal = true;*/

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

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
        movementInput = direction;
    }

    void FixedUpdate()
    {
        Vector2 dir = movementInput;

         // sin diagonales (si se pulsan dos ejes, gana el horizontal)
        if (dir.x != 0 && dir.y != 0)
            dir.y = 0;
            
        Vector2 velocidad = dir * speed;
 
        
        // mientras avanza en un eje, se centra en el otro
        // para entrar recto en los pasillos y no engancharse en las esquinas
        Vector2 centro = grid.GetCellCenterWorld(grid.WorldToCell(rb.position));
 
        if (dir.x != 0)
            velocidad.y = (centro.y - rb.position.y) * 10f;
 
        if (dir.y != 0)
            velocidad.x = (centro.x - rb.position.x) * 10f;
 
        rb.linearVelocity = velocidad;
    }
    
    
}
