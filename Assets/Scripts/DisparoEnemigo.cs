using UnityEngine;

public class DisparoEnemigo : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject prefabProyectil; // Arrastra aquí tu prefab de proyectil
    public Transform puntoDeDisparo;  // Desde dónde sale el proyectil (puede ser el mismo enemigo)
    public float tiempoEntreDisparos = 1.5f; // Cadencia de fuego
    
    private float tiempoUltimoDisparo;
    private Transform jugador;
    private bool jugadorEnRango = false;

    void Start()
    {
        // Buscamos al jugador automáticamente por su Tag (asegúrate de que tu jugador tenga el Tag "Player")
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }
    }

    void Update()
    {
        if (jugadorEnRango && jugador != null)
        {
            // Controlar la cadencia de disparo
            if (Time.time >= tiempoUltimoDisparo + tiempoEntreDisparos)
            {
                Disparar();
                tiempoUltimoDisparo = Time.time;
            }
        }
    }

    void Disparar()
    {
        if (prefabProyectil == null || puntoDeDisparo == null) return;

        // 1. Instanciar el proyectil
        GameObject proyectilInstancia = Instantiate(prefabProyectil, puntoDeDisparo.position, Quaternion.identity);

        // 2. Calcular la dirección hacia el jugador
        Vector2 direccion = (jugador.position - puntoDeDisparo.position).normalized;

        // 3. Configurar la dirección en el script del proyectil
        ProyectilEnemigo scriptProyectil = proyectilInstancia.GetComponent<ProyectilEnemigo>();
        if (scriptProyectil != null)
        {
            scriptProyectil.ConfigurarDireccion(direccion);
        }
    }

    // Detectar cuando el jugador entra al radio del CircleCollider2D (Trigger)
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = true;
        }
    }

    // Detectar cuando el jugador sale del radio
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = false;
        }
    }
}