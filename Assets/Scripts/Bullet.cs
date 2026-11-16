using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;
    public int dano = 10;

    private Rigidbody2D rb;
    private bool usada;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Se mueve hacia transform.right (cambia a transform.up si tu sprite apunta hacia arriba)
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (usada) return;
        if (other.CompareTag("Player")) return;

        // Los triggers (rangos de detección, zonas...) no cuentan como impacto
        if (other.isTrigger) return;

        IDanable objetivo = other.GetComponentInParent<IDanable>();
        if (objetivo != null) objetivo.RecibirDano(dano);

        // Impacta contra enemigos, jefe o cualquier cosa sólida (casas, muros...)
        usada = true;
        Destroy(gameObject);
    }
}
