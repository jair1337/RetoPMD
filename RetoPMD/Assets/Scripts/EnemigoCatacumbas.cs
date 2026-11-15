using UnityEngine;

public class EnemigoCatacumbas : MonoBehaviour
{
    [Header("Movimiento y Detección")]
    public Transform jugador;
    public float velocidad = 1.8f;
    public float distanciaDeteccion = 5f;

    [Header("Patrulla (Ideal para el Murciélago)")]
    public bool patrullarSiEstaLejos = false;
    public float radioPatrulla = 2.5f;
    private Vector2 puntoInicial;
    private Vector2 destinoPatrulla;
    private float tiempoCambioRumbo;

    [Header("Vida y Transformación")]
    public int vidaMaxima = 2;
    public string nombreAnimacionMuerte = "muerteAPlanta";
    public int danoAlJugador = 1;

    private int vidaActual;
    private Animator animador;
    private bool estaMuerto = false;

    void Start()
    {
        vidaActual = vidaMaxima;
        animador = GetComponent<Animator>();
        puntoInicial = transform.position;
        ElegirNuevoDestinoPatrulla();

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
            // Persigue al jugador
            if (animador != null) animador.SetBool("isMoving", true);
            transform.position = Vector2.MoveTowards(transform.position, jugador.position, velocidad * Time.deltaTime);
        }
        else if (patrullarSiEstaLejos)
        {
            // Vuela dando vueltas por su zona
            if (animador != null) animador.SetBool("isMoving", true);
            transform.position = Vector2.MoveTowards(transform.position, destinoPatrulla, (velocidad * 0.6f) * Time.deltaTime);

            tiempoCambioRumbo -= Time.deltaTime;
            if (Vector2.Distance(transform.position, destinoPatrulla) < 0.2f || tiempoCambioRumbo <= 0f)
            {
                ElegirNuevoDestinoPatrulla();
            }
        }
        else
        {
            // Se queda quieto (comportamiento de la rata)
            if (animador != null) animador.SetBool("isMoving", false);
        }
    }

    void ElegirNuevoDestinoPatrulla()
    {
        destinoPatrulla = puntoInicial + Random.insideUnitCircle * radioPatrulla;
        tiempoCambioRumbo = Random.Range(1.5f, 3f);
    }

    public void RecibirDanio(int cantidad)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;

        if (vidaActual <= 0)
        {
            ConvertirEnPlanta();
        }
    }

    void ConvertirEnPlanta()
    {
        estaMuerto = true;

        if (animador != null && !string.IsNullOrEmpty(nombreAnimacionMuerte))
        {
            animador.Play(nombreAnimacionMuerte);
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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (estaMuerto) return;

        if (collision.CompareTag("Player"))
        {
            Vida scriptVidaJugador = collision.GetComponent<Vida>();
            if (scriptVidaJugador != null)
            {
                scriptVidaJugador.TakeDamage(danoAlJugador);
            }
        }
    }
}
