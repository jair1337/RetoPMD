using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Enemigo del poblado: deambula cerca de su punto de origen y, si el jugador entra en
// su radio de ataque, se acerca un poco y le dispara. Se puede matar con las balas del jugador.
// Sustituye a DisparoEnemigo (si lo tiene, copia sus referencias y lo desactiva).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoAI : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public int vidaMaxima = 30;
    public UnityEvent alMorir;

    [Header("Patrulla")]
    public float radioPatrulla = 3f;
    public float velocidadPatrulla = 1.5f;
    public Vector2 esperaMinMax = new Vector2(0.5f, 2f);

    [Header("Ataque")]
    public float radioAtaque = 7f;
    [Tooltip("Se acerca hasta esta distancia del jugador y se queda quieto.")]
    public float distanciaPreferida = 4f;
    public float velocidadAtaque = 1.2f;
    public float tiempoEntreDisparos = 1.5f;
    public GameObject prefabProyectil;
    public Transform puntoDeDisparo;
    public int danoProyectil = 15;
    [Tooltip("Proyectiles por disparo (abanico).")]
    public int rafaga = 1;
    public float separacionRafaga = 12f;
    [Range(0f, 20f)] public float dispersion = 3f;
    [Tooltip("Segundos entre que empieza la animación de ataque y sale el proyectil. Si hay animación de ataque se usa su 'Momento Impacto'.")]
    public float retardoDisparo = 0.15f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    AnimadorSprites anim;
    Transform jugador;
    Vector2 home, destino;
    float esperaHasta, proximoDisparo, tiempoDestino;
    bool moviendo, muerto;
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

        // Migración desde DisparoEnemigo
        DisparoEnemigo viejo = GetComponent<DisparoEnemigo>();
        if (viejo != null)
        {
            if (prefabProyectil == null) prefabProyectil = viejo.prefabProyectil;
            if (puntoDeDisparo == null) puntoDeDisparo = viejo.puntoDeDisparo;
            viejo.enabled = false;
        }

        // Los triggers grandes de detección ya no hacen falta (y bloquearían el daño de las balas)
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

    void Start()
    {
        vida = vidaMaxima;
        home = transform.position;
        destino = home;
        esperaHasta = Time.time + Random.Range(esperaMinMax.x, esperaMinMax.y);

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) jugador = go.transform;
    }

    void OnEnable()
    {
        ObjetivosJugador.Registrar(transform);
    }

    void OnDisable()
    {
        ObjetivosJugador.Quitar(transform);
    }

    void FixedUpdate()
    {
        if (muerto) return;

        if (anim != null) anim.SetMoviendo(rb.linearVelocity.sqrMagnitude > 0.01f);

        if (jugador != null)
        {
            float dist = Vector2.Distance(rb.position, jugador.position);
            if (dist <= radioAtaque)
            {
                Atacar(dist);
                return;
            }
        }
        Patrullar();
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

        rb.linearVelocity = dist > distanciaPreferida ? haciaJugador.normalized * velocidadAtaque : Vector2.zero;

        if (Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + tiempoEntreDisparos;

            if (anim != null)
            {
                anim.Atacar();
                float espera = anim.TieneClipAtacar ? anim.RetardoImpacto : retardoDisparo;
                StartCoroutine(DispararConRetardo(espera));
            }
            else
            {
                Disparar();
            }
        }
    }

    void Orientar(float dx)
    {
        if (sr != null && Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0f;
    }

    IEnumerator DispararConRetardo(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (!muerto && jugador != null) Disparar();
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;

        Vector2 origen = puntoDeDisparo != null ? (Vector2)puntoDeDisparo.position : (Vector2)transform.position;
        Vector2 d = (Vector2)jugador.position - origen;
        float baseAng = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        for (int i = 0; i < Mathf.Max(1, rafaga); i++)
        {
            float ang = baseAng + (i - (Mathf.Max(1, rafaga) - 1) * 0.5f) * separacionRafaga
                        + Random.Range(-dispersion, dispersion);
            float rad = ang * Mathf.Deg2Rad;

            GameObject go = Instantiate(prefabProyectil, origen, Quaternion.identity);
            ProyectilEnemigo p = go.GetComponent<ProyectilEnemigo>();
            if (p != null)
            {
                p.dano = danoProyectil;
                p.ConfigurarDireccion(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
            }
        }
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
