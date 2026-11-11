using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    public float velocidad = 10f;
    public float tiempoDeVida = 3f; // Tiempo antes de destruirse automáticamente
    private Vector2 direccion;

    public void ConfigurarDireccion(Vector2 nuevaDireccion)
    {
        direccion = nuevaDireccion.normalized;
        
        // Opcional: Rotar el proyectil hacia la dirección del movimiento
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    void Start()
    {
        // Destruir el proyectil después de X segundos para evitar que vuele por siempre
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        // Mover el proyectil en línea recta
        transform.Translate(Vector2.right * velocidad * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Si choca con el jugador
        if (collision.CompareTag("Player"))
        {
            // Intentamos obtener el script de vida del jugador
            Vida playerHealth = collision.GetComponent<Vida>();
            
            if (playerHealth != null)
            {
                // Le pasamos el daño (por ejemplo, 20 de daño, puedes crear una variable pública para esto)
                playerHealth.TakeDamage(20); 
            }

            // Destruimos el proyectil al impactar
            Destroy(gameObject);
        }
        
        // Opcional: Si quieres que la bala también explote al chocar con paredes o suelo
        else if (collision.CompareTag("Ground") || collision.CompareTag("Obstaculo"))
        {
            Destroy(gameObject);
        }
    }
}