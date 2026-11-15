using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Interruptor del poblado. Se detecta por distancia al jugador (no necesita collider).
// Al activarse enciende una luz 2D (como la del jugador). El sprite es opcional.
public class Interruptor : MonoBehaviour
{
    public enum Modo { AlTocar, PulsarE }

    [Header("Activación")]
    public Modo modo = Modo.AlTocar;
    public float radioActivacion = 1.2f;
    [Tooltip("Cartel que aparece al acercarte (solo en modo PulsarE).")]
    public string mensajeTeclado = "Presiona E";
    public string mensajeMovil = "Toca para activar";

    [Header("Visual (opcional)")]
    [Tooltip("Si se deja vacío se busca un SpriteRenderer en este objeto o en sus hijos.")]
    public SpriteRenderer render;
    public Sprite spriteEncendido;
    public Color colorApagado = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color colorEncendido = new Color(0.4f, 1f, 1f, 1f);

    [Header("Luz 2D")]
    [Tooltip("Si no hay ninguna Light2D en el objeto, se crea una automáticamente.")]
    public bool crearLuzSiFalta = true;
    public Light2D luz;
    public Color colorLuz = new Color(0.5f, 0.9f, 1f, 1f);
    public float intensidadLuz = 1.2f;
    public float radioLuz = 5f;
    public float duracionEncendido = 0.8f;

    [Header("Recompensa")]
    [Tooltip("Vida que recupera el jugador al activar este interruptor (0 = nada).")]
    public int curacion = 20;

    [Header("Eventos")]
    public UnityEvent alActivar;

    public bool Activado { get; private set; }
    public event Action<Interruptor> AlEncenderse;

    Transform jugador;
    Vida vidaJugador;
    float intensidadObjetivo;

    void Awake()
    {
        if (render == null) render = GetComponent<SpriteRenderer>();
        if (render == null) render = GetComponentInChildren<SpriteRenderer>();
        if (render != null) render.color = colorApagado;

        if (luz == null) luz = GetComponentInChildren<Light2D>();

        if (luz == null && crearLuzSiFalta)
        {
            GameObject go = new GameObject("LuzInterruptor");
            go.transform.SetParent(transform, false);
            luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = radioLuz;
            luz.pointLightInnerRadius = radioLuz * 0.15f;
            luz.falloffIntensity = 0.5f;
            luz.color = colorLuz;
            intensidadObjetivo = intensidadLuz;
        }
        else if (luz != null)
        {
            // Si pusiste tu propia luz, se usa su intensidad como objetivo
            intensidadObjetivo = luz.intensity;
        }

        if (luz != null) luz.intensity = 0f;
    }

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            jugador = go.transform;
            vidaJugador = go.GetComponent<Vida>();
            if (vidaJugador == null) vidaJugador = go.GetComponentInChildren<Vida>();
        }
    }

    void Update()
    {
        if (Activado || jugador == null) return;

        float r = radioActivacion;
        bool cerca = ((Vector2)jugador.position - (Vector2)transform.position).sqrMagnitude <= r * r;

        if (!cerca)
        {
            PromptInteraccion.Ocultar(this);
            return;
        }

        if (modo == Modo.AlTocar)
        {
            Activar();
            return;
        }

        PromptInteraccion.Mostrar(this, mensajeTeclado, mensajeMovil);

        bool teclaE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (teclaE || ControlesMoviles.ConsumirInteractuar()) Activar();
    }

    void OnDisable()
    {
        PromptInteraccion.Ocultar(this);
    }

    public void Activar()
    {
        if (Activado) return;
        Activado = true;
        PromptInteraccion.Ocultar(this);

        if (render != null)
        {
            if (spriteEncendido != null) render.sprite = spriteEncendido;
            render.color = colorEncendido;
        }

        if (luz != null) StartCoroutine(EncenderLuz());

        if (curacion > 0 && vidaJugador != null) vidaJugador.Curar(curacion);

        alActivar?.Invoke();
        AlEncenderse?.Invoke(this);
    }

    IEnumerator EncenderLuz()
    {
        float t = 0f;
        while (t < duracionEncendido)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duracionEncendido));
            luz.intensity = intensidadObjetivo * k;
            yield return null;
        }

        // Leve parpadeo vivo, como una llama
        while (true)
        {
            luz.intensity = intensidadObjetivo * (1f + 0.08f * Mathf.Sin(Time.time * 3f));
            yield return null;
        }
    }

    // Siempre visible en la Scene, aunque no tengas sprite
    void OnDrawGizmos()
    {
        Gizmos.color = Activado ? Color.green : new Color(1f, 0.85f, 0.2f, 1f);
        Vector3 p = transform.position;
        float s = 0.35f;
        Gizmos.DrawLine(p + Vector3.up * s, p + Vector3.right * s);
        Gizmos.DrawLine(p + Vector3.right * s, p + Vector3.down * s);
        Gizmos.DrawLine(p + Vector3.down * s, p + Vector3.left * s);
        Gizmos.DrawLine(p + Vector3.left * s, p + Vector3.up * s);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
        const int n = 32;
        Vector3 prev = transform.position + new Vector3(radioActivacion, 0f, 0f);
        for (int i = 1; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 next = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radioActivacion;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}