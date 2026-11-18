using UnityEngine;

// Escudo del jefe. Ponlo en el objeto "Escudo" (el que tiene el Animator con su animación).
// - Sigue al jefe (también durante el dash).
// - Tiene un collider trigger propio: las balas del jugador lo golpean, le hacen daño al jefe y el escudo
//   se pone rojo un momento (la animación del Animator no se toca).
// - Se desvanece cuando el jefe muere.
public class EscudoJefe : MonoBehaviour, IDanable
{
    [Header("Referencias (se buscan solas)")]
    public BossVida boss;

    [Header("Seguimiento")]
    public bool seguirAlJefe = true;

    [Header("Golpe")]
    public Color colorGolpe = new Color(1f, 0.25f, 0.25f, 1f);
    public float duracionGolpe = 0.15f;

    [Header("Collider")]
    [Tooltip("Si el objeto no tiene collider, se crea uno circular (trigger) ajustado al sprite.")]
    public bool crearColliderSiFalta = true;
    [Range(0.3f, 1.2f)] public float factorRadio = 0.9f;

    [Header("Al morir el jefe")]
    public float duracionDesvanecer = 1f;

    SpriteRenderer sr;
    Color colorBase = Color.white;
    Vector3 desfase;
    float golpeHasta = -1f;
    float desvanecerDesde = -1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) colorBase = sr.color;
    }

    void Start()
    {
        if (boss == null) boss = FindFirstObjectByType<BossVida>(FindObjectsInactive.Include);

        if (boss != null)
        {
            desfase = transform.position - boss.transform.position;
            boss.HaMuerto += AlMorirJefe;
        }

        if (crearColliderSiFalta && GetComponent<Collider2D>() == null && sr != null)
        {
            CircleCollider2D cc = gameObject.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            float escala = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            cc.radius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y) / Mathf.Max(0.0001f, escala) * factorRadio;
        }
    }

    void OnDestroy()
    {
        if (boss != null) boss.HaMuerto -= AlMorirJefe;
    }

    void AlMorirJefe()
    {
        desvanecerDesde = Time.time;
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;
    }

    // Las balas del jugador llaman a esto (IDanable)
    public void RecibirDano(int dano)
    {
        if (boss != null) boss.RecibirDano(dano);   // el daño va a la vida del jefe (él también destella)
        golpeHasta = Time.time + duracionGolpe;
    }

    // En LateUpdate para que el Animator no pise el color
    void LateUpdate()
    {
        if (seguirAlJefe && boss != null)
            transform.position = boss.transform.position + desfase;

        if (sr == null) return;

        Color c = colorBase;
        if (Time.time < golpeHasta)
        {
            float k = Mathf.Clamp01((golpeHasta - Time.time) / Mathf.Max(0.01f, duracionGolpe));
            c = Color.Lerp(colorBase, colorGolpe, k);
        }

        if (desvanecerDesde >= 0f)
            c.a = colorBase.a * (1f - Mathf.Clamp01((Time.time - desvanecerDesde) / Mathf.Max(0.01f, duracionDesvanecer)));

        sr.color = c;
    }
}
