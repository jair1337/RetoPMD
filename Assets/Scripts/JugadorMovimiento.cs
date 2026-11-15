using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        movement = Vector2.zero;

        // Teclado (WASD o flechas)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                movement.y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                movement.y = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                movement.x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                movement.x = 1f;
        }

        // Normalizamos el vector para evitar que en diagonal se mueva más rápido
        movement = movement.normalized;

        // Joystick / botones táctiles (si el teclado no se está usando)
        if (movement == Vector2.zero && ControlesMoviles.Movimiento.sqrMagnitude > 0.0001f)
            movement = Vector2.ClampMagnitude(ControlesMoviles.Movimiento, 1f);

        // Animator: IsWalking true si nos movemos
        bool isMoving = movement != Vector2.zero;
        if (animator != null) animator.SetBool("IsWalking", isMoving);

        // Voltear el sprite (Izquierda / Derecha)
        if (spriteRenderer != null && Mathf.Abs(movement.x) > 0.01f)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}
