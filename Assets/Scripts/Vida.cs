using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Vida : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    // REFERENCIA A LA UI
    public Slider VidaSlider;

    [Header("Invulnerabilidad")]
    [Tooltip("Segundos sin recibir daño tras un golpe.")]
    public float tiempoInvulnerable = 1f;
    [Tooltip("Parpadeos por segundo mientras es invulnerable.")]
    public float frecuenciaParpadeo = 10f;
    [Range(0f, 1f)] public float alphaParpadeo = 0.3f;

    [Header("Muerte")]
    public bool reiniciarEscenaAlMorir = true;
    public float retardoReinicio = 2f;

    [Header("Eventos")]
    public UnityEvent alRecibirDano;
    public UnityEvent alMorir;

    float invulnerableHasta;
    bool muerto;
    SpriteRenderer sr;
    Color colorBase = Color.white;

    public int VidaActual => currentHealth;
    public bool EstaMuerto => muerto;
    public bool EsInvulnerable => Time.time < invulnerableHasta;

    void Start()
    {
        currentHealth = maxHealth;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) colorBase = sr.color;

        ActualizarUI(true);
    }

    public void TakeDamage(int damage)
    {
        if (muerto || damage <= 0 || EsInvulnerable) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        ActualizarUI(false);
        alRecibirDano?.Invoke();

        if (currentHealth <= 0)
        {
            Morir();
            return;
        }

        invulnerableHasta = Time.time + tiempoInvulnerable;
        StartCoroutine(Parpadeo());
    }

    public void Curar(int cantidad)
    {
        if (muerto) return;
        currentHealth = Mathf.Clamp(currentHealth + cantidad, 0, maxHealth);
        ActualizarUI(false);
    }

    void ActualizarUI(bool inicial)
    {
        if (VidaSlider == null) return;
        if (inicial) VidaSlider.maxValue = maxHealth;
        VidaSlider.value = currentHealth;
    }

    IEnumerator Parpadeo()
    {
        if (sr == null) yield break;

        while (Time.time < invulnerableHasta && !muerto)
        {
            bool visible = Mathf.Sin(Time.time * frecuenciaParpadeo * Mathf.PI * 2f) > 0f;
            Color c = colorBase;
            c.a = colorBase.a * (visible ? 1f : alphaParpadeo);
            sr.color = c;
            yield return null;
        }
        sr.color = colorBase;
    }

    void Morir()
    {
        muerto = true;
        Debug.Log("Jugador Muerto");

        if (sr != null) sr.color = colorBase;

        // Congelamos al jugador sin depender del nombre de su script de movimiento
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        alMorir?.Invoke();

        if (reiniciarEscenaAlMorir) StartCoroutine(Reiniciar());
    }

    IEnumerator Reiniciar()
    {
        yield return new WaitForSeconds(retardoReinicio);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
