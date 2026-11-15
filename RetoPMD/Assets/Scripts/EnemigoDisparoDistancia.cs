using UnityEngine;

public class EnemigoDisparoDistancia : MonoBehaviour
{
    [Header("Detección y Rango")]
    public Transform jugador;
    public float distanciaDeteccion = 6f;

    [Header("Configuración de Disparo")]
    public GameObject prefabProyectil;     // Arrastra aquí el prefab que tiene DisparoCatacumbas
    public Transform puntoDeDisparo;       // Desde dónde sale la bala (opcional)
    public float tiempoEntreDisparos = 2f; // Cadencia de disparo
    public int danioProyectil = 20;        // Daño que hace este proyectil

    [Header("Vida y Muerte (Opcional)")]
    public int vidaMaxima = 2;
    public string nombreAnimacionMuerte = "muerteAPlanta";
    public GameObject prefabAlMorir;       // Objeto que deja al morir (ej. planta)

    private int vidaActual;
    private Animator animador;
    private bool estaMuerto = false;
    private float tiempoUltimoDisparo;

    void Start()
    {
        vidaActual = vidaMaxima;
        animador = GetComponent<Animator>();

        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
        }
    }

    void Update()
    {
        if (estaMuerto || jugador == null) return;

        float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (distanciaAlJugador <= distanciaDeteccion)
        {
            if (animador != null) animador.SetBool("isMoving", false);

            if (Time.time >= tiempoUltimoDisparo + tiempoEntreDisparos)
            {
                Disparar();
                tiempoUltimoDisparo = Time.time;
            }
        }
        else
        {
            if (animador != null) animador.SetBool("isMoving", false);
        }
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;

        Vector2 posicionSpawn = puntoDeDisparo != null ? puntoDeDisparo.position : transform.position;

        Vector2 direccion = ((Vector2)jugador.position - posicionSpawn).normalized;
        float angle = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        Quaternion rotacionBala = Quaternion.Euler(0, 0, angle);

        GameObject proyectilInstancia = Instantiate(prefabProyectil, posicionSpawn, rotacionBala);

        // BUSCA EL SCRIPT DisparoCatacumbas EN LA BALA
        DisparoCatacumbas scriptProyectil = proyectilInstancia.GetComponent<DisparoCatacumbas>();
        if (scriptProyectil != null)
        {
            scriptProyectil.ConfigurarProyectil(direccion, danioProyectil);
        }
        else
        {
            Debug.LogError("¡El prefab de la bala asignado en " + gameObject.name + " no tiene el script DisparoCatacumbas!", prefabProyectil);
        }
    }

    public void RecibirDanio(int cantidad)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        estaMuerto = true;
        enabled = false;

        if (animador != null && !string.IsNullOrEmpty(nombreAnimacionMuerte))
        {
            animador.Play(nombreAnimacionMuerte);
        }

        if (prefabAlMorir != null)
        {
            Instantiate(prefabAlMorir, transform.position, Quaternion.identity);
        }

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (estaMuerto) return;

        if (collision.GetComponent<Bullet>() != null || collision.CompareTag("Bullet") || collision.name.Contains("Bullet"))
        {
            RecibirDanio(1);
            Destroy(collision.gameObject);
        }
    }
}