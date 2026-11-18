using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Vida del jefe. Necesita un collider NO trigger (se añade un CircleCollider2D): ajusta su radio
// al tamaño del jefe. Las balas del jugador le hacen daño a través de IDanable.
[RequireComponent(typeof(CircleCollider2D))]
public class BossVida : MonoBehaviour, IDanable
{
    public int vidaMaxima = 1500;
    [Tooltip("Slider de UI con la vida del jefe. Opcional.")]
    public Slider barra;
    [Tooltip("Invulnerable hasta que empiece el combate (lo gestiona EncuentroJefe).")]
    public bool invulnerable = true;

    [Header("Fases")]
    [Range(0f, 1f)] public float umbralFase2 = 0.66f;
    [Range(0f, 1f)] public float umbralFase3 = 0.33f;
    [Tooltip("Última fase: se activa cuando la vida baja de este porcentaje.")]
    [Range(0f, 1f)] public float umbralFase4 = 0.1f;

    [Header("Feedback")]
    public Color colorGolpe = new Color(1f, 0.5f, 0.5f, 1f);

    [Header("Eventos")]
    public UnityEvent<int> alCambiarFase;
    public UnityEvent alMorir;

    public event Action HaMuerto;
    public event Action Activada;

    public int VidaActual { get; private set; }
    public int Fase { get; private set; } = 1;
    public bool EstaMuerto { get; private set; }
    public float Porcentaje => vidaMaxima > 0 ? (float)VidaActual / vidaMaxima : 0f;

    SpriteRenderer sr;
    Color colorBase = Color.white;

    void Reset()
    {
        CircleCollider2D c = GetComponent<CircleCollider2D>();
        c.radius = 1.5f;
        c.isTrigger = false;
    }

    void Awake()
    {
        VidaActual = vidaMaxima;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) colorBase = sr.color;

        if (barra != null)
        {
            barra.maxValue = vidaMaxima;
            barra.value = VidaActual;
        }
    }

    public void Activar()
    {
        invulnerable = false;
        ObjetivosJugador.Registrar(transform);
        if (Activada != null) Activada();
    }

    public void RecibirDano(int dano)
    {
        if (invulnerable || EstaMuerto || dano <= 0) return;

        VidaActual = Mathf.Max(0, VidaActual - dano);
        if (barra != null) barra.value = VidaActual;

        int nueva = Porcentaje > umbralFase2 ? 1 : (Porcentaje > umbralFase3 ? 2 : (Porcentaje > umbralFase4 ? 3 : 4));
        if (nueva != Fase)
        {
            Fase = nueva;
            alCambiarFase?.Invoke(Fase);
        }

        if (VidaActual <= 0)
        {
            EstaMuerto = true;
            ObjetivosJugador.Quitar(transform);
            alMorir?.Invoke();
            HaMuerto?.Invoke();
            return;
        }

        if (sr != null) StartCoroutine(Destello());
    }

    IEnumerator Destello()
    {
        sr.color = colorGolpe;
        yield return new WaitForSeconds(0.06f);
        if (sr != null) sr.color = colorBase;
    }
}
