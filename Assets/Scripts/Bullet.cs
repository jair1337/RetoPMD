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

        // Enemigos, jefe y escudo del jefe (el escudo es un trigger para no bloquear al jugador)
        IDanable objetivo = other.GetComponentInParent<IDanable>();
        if (objetivo != null)
        {
            objetivo.RecibirDano(dano);
            usada = true;
            Destroy(gameObject);
            return;
        }

        // Los demás triggers (rangos de detección, zonas...) no cuentan como impacto
        if (other.isTrigger) return;

        // Cualquier cosa sólida (casas, muros...)
        usada = true;
        Destroy(gameObject);
    }
}
