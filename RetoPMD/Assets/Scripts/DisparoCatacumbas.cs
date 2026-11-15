using UnityEngine;

public class DisparoCatacumbas : MonoBehaviour
{
    public float velocidad = 10f;
    public float tiempoDeVida = 5f;

    private Rigidbody2D rb;
    private int danio;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ConfigurarProyectil(Vector2 nuevaDireccion, int nuevoDanio)
    {
        danio = nuevoDanio;

        rb.linearVelocity = nuevaDireccion.normalized * velocidad;

        float angulo = Mathf.Atan2(
            nuevaDireccion.y,
            nuevaDireccion.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    private void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vida vida = collision.gameObject.GetComponent<Vida>();

            if (vida != null)
            {
                vida.TakeDamage(danio);
            }

            Destroy(gameObject);
        }
    }
}