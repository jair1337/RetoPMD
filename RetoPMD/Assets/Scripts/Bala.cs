using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Forzamos a que la bala se mueva en la dirección local "hacia la derecha" (transform.right) 
        // o puedes cambiarlo a transform.up si tu bala visualmente apunta hacia arriba.
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed; // Usa rb.velocity si estás en una versión anterior a Unity 6
        }

        // Destruye la bala después de unos segundos para que no sature el juego
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Evita que la bala colisione con el propio jugador si este tiene un collider sólido
        if (collision.CompareTag("Player")) return;

        // Destruye la bala al impactar con cualquier otra cosa
        Destroy(gameObject);
    }
}