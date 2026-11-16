using UnityEngine;
using UnityEngine.Events;

// Pon este script en un objeto vacío colocado en el "círculo rojo" de entrada a la arena.
// - Al empezar la partida: arena DESACTIVADA y jefe quieto e invulnerable.
// - Cuando el jugador entra en el radio: se activa la arena (Mask + collider) y empieza el combate.
// - Cuando el jefe muere: se detienen los ataques y la arena se abre de nuevo.
public class EncuentroJefe : MonoBehaviour
{

    [Header("Referencias (se buscan solas si están vacías)")]
    public Arena arena;
    public BossController boss;
    public BossVida bossVida;
    public Transform jugador;
    [Header("Audio")]
    public AudioSource bossMusic;

    [Header("Activación")]
    public float radioActivacion = 2.5f;

    [Header("Final")]
    public bool abrirArenaAlMatarJefe = true;
    [Tooltip("Objeto de UI con la barra de vida del jefe. Se muestra al empezar. Opcional.")]
    public GameObject barraVidaJefe;

    [Header("Eventos")]
    public UnityEvent alIniciar;
    public UnityEvent alTerminar;

    public bool Iniciado { get; private set; }
    public bool Terminado { get; private set; }

    void Awake()
    {
        if (arena == null) arena = FindFirstObjectByType<Arena>(FindObjectsInactive.Include);
        if (boss == null) boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
        if (bossVida == null && boss != null) bossVida = boss.GetComponent<BossVida>();

        if (arena != null) arena.gameObject.SetActive(false);

        if (boss != null)
        {
            boss.atacar = false;
            if (arena != null) boss.arena = arena;
        }

        if (bossVida != null) bossVida.invulnerable = true;
        if (barraVidaJefe != null) barraVidaJefe.SetActive(false);

        // Barra de vida en el canvas: si no usas la tuya, se crea una automáticamente
        if (bossVida != null && bossVida.barra == null && barraVidaJefe == null
            && bossVida.GetComponent<BarraVidaJefeUI>() == null)
        {
            bossVida.gameObject.AddComponent<BarraVidaJefeUI>();
        }
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.transform;
        }

        if (bossVida != null) bossVida.HaMuerto += Terminar;
    }

    void OnDestroy()
    {
        if (bossVida != null) bossVida.HaMuerto -= Terminar;
    }

    void Update()
    {
        if (Iniciado || jugador == null) return;

        float r = radioActivacion;
        if (((Vector2)jugador.position - (Vector2)transform.position).sqrMagnitude <= r * r) Iniciar();
    }

    public void Iniciar()
    {
        if (Iniciado) return;
        Iniciado = true;

        if (arena != null) arena.gameObject.SetActive(true);
        if (boss != null)
        {
            if (arena != null) boss.arena = arena;
            boss.IniciarCombate();
        }
        if (bossVida != null) bossVida.Activar();
        if (barraVidaJefe != null) barraVidaJefe.SetActive(true);

        if (bossMusic != null && !bossMusic.isPlaying) bossMusic.Play();

        alIniciar?.Invoke();
    }

    void Terminar()
    {
        if (Terminado) return;
        Terminado = true;

        if (boss != null) boss.DetenerCombate();
        if (abrirArenaAlMatarJefe && arena != null) arena.gameObject.SetActive(false);
        if (barraVidaJefe != null) barraVidaJefe.SetActive(false);

        if (bossMusic != null) bossMusic.Stop();

        alTerminar?.Invoke();
        Eco99.ProgresionEco99.CompletarNivel();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 1f);
        const int n = 40;
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
