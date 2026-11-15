using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    public float velocidad = 10f;
    public float tiempoDeVida = 3f; // Tiempo antes de destruirse automáticamente
    public int dano = 20;
    private Vector2 direccion;

    public void ConfigurarDireccion(Vector2 nuevaDireccion)
    {
        direccion = nuevaDireccion.normalized;

        // Rotar el proyectil hacia la dirección del movimiento
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        transform.Translate(Vector2.right * velocidad * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Vida playerHealth = collision.GetComponent<Vida>();
            if (playerHealth == null) playerHealth = collision.GetComponentInParent<Vida>();

            if (playerHealth != null) playerHealth.TakeDamage(dano);

            Destroy(gameObject);
            return;
        }

        // No chocar con enemigos ni con el jefe (ni consigo mismo al salir del cañón)
        if (collision.GetComponentInParent<IDanable>() != null) return;

        // Paredes, casas, muros... (los triggers se ignoran; así no hace falta ningún tag)
        if (!collision.isTrigger) Destroy(gameObject);
    }
}
