using UnityEngine;

public class DisparoCatacumbas : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 10f;
    public float tiempoDeVida = 3f;

    private Vector2 direccion;
    private int danio = 20;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Este es el método que llama el enemigo al disparar
    public void ConfigurarProyectil(Vector2 nuevaDireccion, int nuevoDanio)
    {
        direccion = nuevaDireccion.normalized;
        danio = nuevoDanio;

        // Rotar el proyectil hacia la dirección del movimiento
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        if (rb != null)
        {
            rb.linearVelocity = direccion * velocidad;
        }
    }

    void Start()
    {
        if (rb != null && direccion != Vector2.zero)
        {
            rb.linearVelocity = direccion * velocidad;
        }

        Destroy(gameObject, tiempoDeVida);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Impacto con el jugador
        if (collision.CompareTag("Player"))
        {
            Vida playerHealth = collision.GetComponent<Vida>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(danio);
            }

            Destroy(gameObject);
        }
        // Impacto con paredes, suelo u obstáculos de las catacumbas
        else if (collision.CompareTag("Ground") || collision.CompareTag("Obstaculo") || collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject);
        }
    }
}