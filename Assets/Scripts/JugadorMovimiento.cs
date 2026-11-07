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

        // Comprobamos si el teclado está conectado y leemos las teclas WASD o flechas directamente
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

        // ==========================================
        // 1. ACTUALIZAR EL ANIMATOR CON UN BOOLEANO
        // ==========================================
        // Si nos estamos moviendo (movement != Vector2.zero), IsWalking será true. Si no, false.
        bool isMoving = movement != Vector2.zero;
        animator.SetBool("IsWalking", isMoving); // Asegúrate de que el parámetro en Unity se llame exactamente así (o cámbialo por el nombre que le hayas puesto)

        // ==========================================
        // 2. VOLTEAR EL SPRITE (Izquierda / Derecha)
        // ==========================================
        if (movement.x != 0)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}