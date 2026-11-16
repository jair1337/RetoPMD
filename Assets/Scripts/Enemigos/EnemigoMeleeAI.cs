using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Enemigo cuerpo a cuerpo (melee): deambula cerca de su punto de origen y, si el jugador
// entra en su radio de ataque, se acerca y le golpea. El daño cae en el "momento de impacto"
// de la animación de ataque (si la esquivas alejándote, no te toca).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoMeleeAI : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public int vidaMaxima = 30;
    public UnityEvent alMorir;

    [Header("Patrulla")]
    public float radioPatrulla = 3f;
    public float velocidadPatrulla = 1.5f;
    public Vector2 esperaMinMax = new Vector2(0.5f, 2f);

    [Header("Ataque Cuerpo a Cuerpo")]
    public float radioAtaque = 7f;
    [Tooltip("Distancia a la que se detiene respecto al jugador para atacar.")]
    public float distanciaPreferida = 0.8f; // Cerca del jugador para melee
    public float velocidadAtaque = 2f;
    public int danoMelee = 10;
    [Tooltip("Tiempo mínimo entre cada golpe.")]
    public float tiempoEntreAtaques = 1f;
    [Tooltip("Segundos entre que empieza el golpe y cae el daño (si hay animación de ataque se usa su 'Momento Impacto').")]
    public float retardoImpacto = 0.25f;
    [Tooltip("Si en el momento del impacto estás más lejos que Distancia Preferida + este margen, lo esquivas.")]
    public float margenGolpe = 0.6f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    AnimadorSprites anim;
    Transform jugador;
    Vida vidaJugador;
    Vector2 home, destino;
    float esperaHasta, proximoAtaque, tiempoDestino;
    bool moviendo, muerto, golpeando;
    int vida;
    Color colorBase = Color.white;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 50f;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) colorBase = sr.color;
        anim = GetComponent<AnimadorSprites>();

        // Asegurar que tenga un colisionador sólido para las balas del jugador
        bool haySolido = false;
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger) haySolido = true;
            else c.enabled = false;
        }
        if (!haySolido)
        {
            CircleCollider2D cc = gameObject.AddComponent<CircleCollider2D>();
            cc.radius = 0.5f;
        }
    }

    void OnEnable()
    {
        ObjetivosJugador.Registrar(transform);
    }

    void OnDisable()
    {
        ObjetivosJugador.Quitar(transform);
    }

    void Start()
    {
        vida = vidaMaxima;
        home = transform.position;
        destino = home;
        esperaHasta = Time.time + Random.Range(esperaMinMax.x, esperaMinMax.y);

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            jugador = go.transform;
            vidaJugador = go.GetComponent<Vida>();
        }
    }

    void FixedUpdate()
    {
        if (muerto) return;

        if (golpeando)
        {
            rb.linearVelocity = Vector2.zero;   // se queda quieto mientras golpea
            if (anim != null) anim.SetMoviendo(false);
            return;
        }

        if (jugador != null)
        {
            float dist = Vector2.Distance(rb.position, jugador.position);
            if (dist <= radioAtaque)
            {
                Atacar(dist);
                if (anim != null) anim.SetMoviendo(rb.linearVelocity.sqrMagnitude > 0.01f);
                return;
            }
        }

        Patrullar();
        if (anim != null) anim.SetMoviendo(rb.linearVelocity.sqrMagnitude > 0.01f);
    }

    void Patrullar()
    {
        if (moviendo)
        {
            Vector2 hacia = destino - rb.position;
            tiempoDestino += Time.fixedDeltaTime;

            if (hacia.magnitude < 0.15f || tiempoDestino > 4f)
            {
                moviendo = false;
                rb.linearVelocity = Vector2.zero;
                esperaHasta = Time.time + Random.Range(esperaMinMax.x, esperaMinMax.y);
            }
            else
            {
                rb.linearVelocity = hacia.normalized * velocidadPatrulla;
                Orientar(hacia.x);
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= esperaHasta)
            {
                destino = home + Random.insideUnitCircle * radioPatrulla;
                tiempoDestino = 0f;
                moviendo = true;
            }
        }
    }

    void Atacar(float dist)
    {
        moviendo = false;
        Vector2 haciaJugador = (Vector2)jugador.position - rb.position;
        Orientar(haciaJugador.x);

        // Si está más lejos que la distancia preferida, se acerca. Si está a distancia de golpe, se detiene.
        rb.linearVelocity = dist > distanciaPreferida ? haciaJugador.normalized * velocidadAtaque : Vector2.zero;

        // Si está lo suficientemente cerca para golpear y ha pasado el tiempo de reutilización
        if (dist <= distanciaPreferida + 0.2f && Time.time >= proximoAtaque)
        {
            float duracion = anim != null ? anim.DuracionAtaque : 0f;
            proximoAtaque = Time.time + Mathf.Max(tiempoEntreAtaques, duracion);
            StartCoroutine(Golpear());
        }
    }

    IEnumerator Golpear()
    {
        golpeando = true;
        if (anim != null) anim.Atacar();

        float espera = (anim != null && anim.TieneClipAtacar) ? anim.RetardoImpacto : retardoImpacto;
        yield return new WaitForSeconds(espera);

        if (!muerto && jugador != null && vidaJugador != null)
        {
            float dist = Vector2.Distance(rb.position, jugador.position);
            if (dist <= distanciaPreferida + margenGolpe)
            {
                vidaJugador.TakeDamage(danoMelee);
                StartCoroutine(EfectoGolpe());
            }
        }

        // Termina el resto de la animación antes de volver a moverse
        if (anim != null && anim.TieneClipAtacar)
            yield return new WaitForSeconds(Mathf.Max(0f, anim.DuracionAtaque - espera));

        golpeando = false;
    }

    IEnumerator EfectoGolpe()
    {
        // Destello al golpear
        if (sr != null)
        {
            sr.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.color = colorBase;
        }
    }

    void Orientar(float dx)
    {
        if (sr != null && Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0f;
    }

    public void RecibirDano(int dano)
    {
        if (muerto) return;

        vida -= dano;
        if (vida <= 0)
        {
            muerto = true;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            ObjetivosJugador.Quitar(transform);
            alMorir?.Invoke();

            float retardoMuerte = anim != null ? anim.Morir() : 0f;
            Destroy(gameObject, retardoMuerte);
            return;
        }

        if (sr != null) StartCoroutine(Destello());
    }

    IEnumerator Destello()
    {
        sr.color = new Color(1f, 0.4f, 0.4f, colorBase.a);
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = colorBase;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? (Vector3)home : transform.position;

        Gizmos.color = Color.green;
        Circulo(centro, radioPatrulla);
        Gizmos.color = Color.red;
        Circulo(transform.position, radioAtaque);

        // Alcance del golpe cuerpo a cuerpo
        Gizmos.color = Color.yellow;
        Circulo(transform.position, distanciaPreferida);
    }

    static void Circulo(Vector3 c, float r)
    {
        const int n = 40;
        Vector3 prev = c + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 next = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
