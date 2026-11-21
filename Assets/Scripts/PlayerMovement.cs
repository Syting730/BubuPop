using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public enum ControlScheme
    {
        InputActions,
        WASD,
        Flechas,
        SoloMando
    }

    public float speed = 5f;

    [Header("Controles")]
    public ControlScheme controls = ControlScheme.InputActions;

    [Tooltip("Mando de este jugador: 0 = primer mando conectado, 1 = segundo... " +
             "-1 = no usa mando. Mismo número que en BombPlacer y Diamante")]
    public int numeroMando = 0;

    public Grid grid;

    private Vector2 movementInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

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

        movementInput = value.Get<Vector2>();
    }

    void Update()
    {
      if (controls == ControlScheme.InputActions)
            return;

        Vector2 direction = Vector2.zero;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (controls == ControlScheme.WASD)
            {
                if (keyboard.wKey.isPressed) direction.y += 1;
                if (keyboard.sKey.isPressed) direction.y -= 1;
                if (keyboard.dKey.isPressed) direction.x += 1;
                if (keyboard.aKey.isPressed) direction.x -= 1;
            }
            else if (controls == ControlScheme.Flechas)
            {
                if (keyboard.upArrowKey.isPressed) direction.y += 1;
                if (keyboard.downArrowKey.isPressed) direction.y -= 1;
                if (keyboard.rightArrowKey.isPressed) direction.x += 1;
                if (keyboard.leftArrowKey.isPressed) direction.x -= 1;
            }
        }

        Gamepad mando = MiMando();

        if (mando != null)
        {
            Vector2 stick = mando.leftStick.ReadValue() + mando.dpad.ReadValue();

            if (stick.magnitude > 0.5f)
            {
                if (Mathf.Abs(stick.x) >= Mathf.Abs(stick.y))
                    direction = new Vector2(Mathf.Sign(stick.x), 0);
                else
                    direction = new Vector2(0, Mathf.Sign(stick.y));
            }
        }

        movementInput = direction;
    }

    Gamepad MiMando()
    {
        if (numeroMando < 0 || numeroMando >= Gamepad.all.Count)
            return null;

        return Gamepad.all[numeroMando];
    }

    void FixedUpdate()
    {
        Vector2 dir = movementInput;

        if (dir.x != 0 && dir.y != 0)
            dir.y = 0;

        Vector2 velocidad = dir * speed;

        Vector2 centro = grid.GetCellCenterWorld(grid.WorldToCell(rb.position));

        if (dir.x != 0)
            velocidad.y = (centro.y - rb.position.y) * 10f;

        if (dir.y != 0)
            velocidad.x = (centro.x - rb.position.x) * 10f;

        rb.linearVelocity = velocidad;
    }
}
