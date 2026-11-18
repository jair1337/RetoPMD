using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cerebro del jefe: encadena los ataques, conoce la arena y reparte daño al jugador.
// El combate lo arranca EncuentroJefe (IniciarCombate) y lo para al morir (DetenerCombate).
public class BossController : MonoBehaviour
{
    [Header("Arena")]
    [Tooltip("Si se deja vacío se busca automáticamente (incluso si está desactivada).")]
    public Arena arena;
    [Tooltip("Solo se usa si no hay Arena.")]
    public float radioArenaManual = 8f;
    [Tooltip("Las balas desaparecen al pasar el borde de la arena más este margen.")]
    public float margenSalida = 1.5f;

    [Header("Jugador")]
    [Tooltip("Si se deja vacío se busca el objeto con tag Player.")]
    public Transform jugador;
    [Tooltip("Radio de la hitbox del jugador (mira el gizmo cian en la Scene).")]
    public float radioJugador = 0.25f;

    [Header("Secuencia de ataques")]
    [Tooltip("Si se deja vacío usa todos los BossAttack de este objeto, en el orden de los componentes.")]
    public BossAttack[] ataques;
    public bool ordenAleatorio = true;
    public float retrasoInicial = 2f;
    public float pausaEntreAtaques = 1.5f;
    [Tooltip("EncuentroJefe lo pone en false al empezar y lo activa al llegar el jugador.")]
    public bool atacar = true;

    [Header("Dificultad por fase (1 = rojo, 2 = naranja, 3 = morado)")]
    [Tooltip("Multiplica la velocidad de las balas y de la rotación de los láseres.")]
    public float velocidadFase2 = 1.2f;
    public float velocidadFase3 = 1.3f;
    [Tooltip("Divide las pausas: entre ataques, entre oleadas y entre disparos.")]
    public float frecuenciaFase2 = 1.3f;
    public float frecuenciaFase3 = 1.5f;
    [Tooltip("Fase 4: la última (la vida baja de Umbral Fase 4 en Boss Vida, ~10 %).")]
    public float velocidadFase4 = 1.5f;
    public float frecuenciaFase4 = 1.5f;

    [Header("Ataques desbloqueables (Rejilla, Orbes, Dash...)")]
    [Tooltip("Probabilidad de que el jefe use uno de los ataques que se desbloquean por fase en lugar de un ataque normal o un combo.")]
    [Range(0f, 1f)] public float probabilidadEspeciales = 0.5f;

    [Tooltip("En la fase 4 el jefe SOLO usa los ataques desbloqueables (Dash, Orbes y Rejilla): sin ataques normales ni combos.")]
    public bool soloEspecialesEnFase4 = true;

    [Header("Fase 3: ataques combinados")]
    [Tooltip("Láseres + Círculos, Lluvia de arriba + Lluvia de abajo, Balas + Círculos.")]
    public bool combinarEnFase3 = true;
    [Range(0f, 1f)] public float probabilidadCombo = 1f;
    [Tooltip("En los combos, los intervalos se alargan por este factor para no saturar la pantalla.")]
    public float relajarCombo = 1.25f;

    [Header("Balas")]
    [Range(0.3f, 1f)] public float factorHitboxBala = 0.8f;
    [Tooltip("Sprite opcional para las balas (apuntando a la derecha). Vacío = círculo de relleno.")]
    public Sprite spriteBala;

    [Header("Vida y animación (opcionales, se buscan solos)")]
    public BossVida bossVida;
    public AnimadorSprites animador;

    Vida vida;
    Vector2 centroCache;
    float limiteSqr;
    Coroutine bucle;
    int ultimoCombo = -1;

    public Vector2 ArenaCentro => arena != null ? (Vector2)arena.transform.position : (Vector2)transform.position;

    public float ArenaRadio
    {
        get
        {
            if (arena == null) return radioArenaManual;
            Vector3 s = arena.transform.lossyScale;
            return arena.radio * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }
    }

    int FaseActual { get { return bossVida != null ? bossVida.Fase : 1; } }

    public float MultiplicadorVelocidad
    {
        get { return FaseActual >= 4 ? velocidadFase4 : (FaseActual == 3 ? velocidadFase3 : (FaseActual == 2 ? velocidadFase2 : 1f)); }
    }

    public float MultiplicadorFrecuencia
    {
        get { return FaseActual >= 4 ? frecuenciaFase4 : (FaseActual == 3 ? frecuenciaFase3 : (FaseActual == 2 ? frecuenciaFase2 : 1f)); }
    }

    public bool EnCombo { get; private set; }

    // Posición inicial del jefe (el centro de la arena): el dash vuelve aquí
    public Vector2 PosicionBase { get; private set; }

    float PausaActual { get { return pausaEntreAtaques / Mathf.Max(0.1f, MultiplicadorFrecuencia); } }

    void Awake()
    {
        BossBulletPool.Reiniciar();
    }

    void Start()
    {
        if (arena == null) arena = FindFirstObjectByType<Arena>(FindObjectsInactive.Include);
        if (bossVida == null) bossVida = GetComponent<BossVida>();
        if (animador == null) animador = GetComponent<AnimadorSprites>();

        if (jugador == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.transform;
        }

        if (jugador != null)
        {
            vida = jugador.GetComponent<Vida>();
            if (vida == null) vida = jugador.GetComponentInChildren<Vida>();
            if (vida == null) vida = jugador.GetComponentInParent<Vida>();
        }

        PosicionBase = transform.position;

        // Usa todos los ataques del objeto; si hay una lista manual, añade los que falten
        BossAttack[] todos = GetComponents<BossAttack>();
        if (ataques == null || ataques.Length == 0)
        {
            ataques = todos;
        }
        else
        {
            List<BossAttack> lista = new List<BossAttack>(ataques);
            foreach (BossAttack a in todos)
                if (!lista.Contains(a)) lista.Add(a);
            ataques = lista.ToArray();
        }

        ActualizarCache();
        bucle = StartCoroutine(Bucle());
    }

    void Update()
    {
        ActualizarCache();
    }

    void ActualizarCache()
    {
        centroCache = ArenaCentro;
        float lim = ArenaRadio + margenSalida;
        limiteSqr = lim * lim;
    }

    // ---------- CONTROL DEL COMBATE ----------

    public void IniciarCombate()
    {
        atacar = true;
        if (bucle == null) bucle = StartCoroutine(Bucle());
    }

    public void DetenerCombate()
    {
        atacar = false;
        EnCombo = false;
        StopAllCoroutines();
        bucle = null;

        if (ataques != null)
            foreach (BossAttack a in ataques)
                if (a != null) a.StopAllCoroutines();

        LimpiarBalas();
        BossSprites.LimpiarEfectos();
    }

    public void LimpiarBalas()
    {
        BossBulletPool.LimpiarActivas();
    }

    IEnumerator Bucle()
    {
        while (!atacar) yield return null;
        yield return new WaitForSeconds(retrasoInicial);

        BossAttack ultimo = null;
        BossAttack ultimoEspecial = null;
        int siguiente = 0;

        while (true)
        {
            if (!atacar || ataques == null || ataques.Length == 0)
            {
                yield return null;
                continue;
            }

            // 1) Ataques que se desbloquean por fase (Rejilla en fase 3; Orbes y Dash en fase 4).
            //    En la fase 4 son los únicos que usa.
            bool soloEspeciales = soloEspecialesEnFase4 && FaseActual >= 4;
            if (soloEspeciales || Random.value < probabilidadEspeciales)
            {
                BossAttack especial = ElegirEspecial(ultimoEspecial);
                if (especial != null)
                {
                    ultimoEspecial = especial;
                    if (animador != null) animador.Atacar();
                    yield return StartCoroutine(especial.Ejecutar(this));
                    yield return new WaitForSeconds(PausaActual);
                    continue;
                }
            }

            // 2) Fase 3 o más: dos ataques a la vez
            if (combinarEnFase3 && FaseActual >= 3 && Random.value <= probabilidadCombo)
            {
                BossAttack[] combo = ElegirCombo();
                if (combo != null)
                {
                    if (animador != null) animador.Atacar();
                    EnCombo = true;
                    Coroutine c1 = StartCoroutine(combo[0].Ejecutar(this));
                    Coroutine c2 = StartCoroutine(combo[1].Ejecutar(this));
                    yield return c1;
                    yield return c2;
                    EnCombo = false;
                    yield return new WaitForSeconds(PausaActual);
                    continue;
                }
            }

            // 3) Ataque normal (los que no están bloqueados por fase)
            List<BossAttack> normales = new List<BossAttack>();
            foreach (BossAttack a in ataques)
                if (a != null && a.isActiveAndEnabled && a.faseMinima <= 1) normales.Add(a);

            if (normales.Count == 0)
            {
                yield return null;
                continue;
            }

            BossAttack elegido;
            if (ordenAleatorio)
            {
                int i = Random.Range(0, normales.Count);
                elegido = normales[i];
                if (normales.Count > 1 && elegido == ultimo) elegido = normales[(i + 1) % normales.Count];
            }
            else
            {
                elegido = normales[siguiente % normales.Count];
                siguiente++;
            }

            ultimo = elegido;
            if (animador != null) animador.Atacar();
            yield return StartCoroutine(elegido.Ejecutar(this));
            yield return new WaitForSeconds(PausaActual);
        }
    }

    // Ataques desbloqueables ya disponibles en la fase actual (evita repetir el anterior si hay más de uno)
    BossAttack ElegirEspecial(BossAttack excluir)
    {
        List<BossAttack> lista = new List<BossAttack>();
        foreach (BossAttack a in ataques)
            if (a != null && a.isActiveAndEnabled && a.faseMinima > 1 && a.faseMinima <= FaseActual) lista.Add(a);

        if (lista.Count == 0) return null;
        if (lista.Count > 1) lista.Remove(excluir);
        return lista[Random.Range(0, lista.Count)];
    }

    // Devuelve una pareja de ataques para ejecutar a la vez, o null si faltan componentes
    BossAttack[] ElegirCombo()
    {
        AtaqueLaseres lase = null;
        AtaqueCirculos circ = null;
        AtaqueBalas bal = null;
        AtaqueLluvia arriba = null;
        AtaqueLluvia abajo = null;

        if (ataques != null)
        {
            foreach (BossAttack a in ataques)
            {
                if (a == null || !a.isActiveAndEnabled || a.faseMinima > FaseActual) continue;

                if (a is AtaqueLaseres) lase = (AtaqueLaseres)a;
                else if (a is AtaqueCirculos) circ = (AtaqueCirculos)a;
                else if (a is AtaqueBalas) bal = (AtaqueBalas)a;
                else if (a is AtaqueLluvia)
                {
                    AtaqueLluvia l = (AtaqueLluvia)a;
                    if (l.origen == AtaqueLluvia.Origen.Arriba) arriba = l;
                    else if (l.origen == AtaqueLluvia.Origen.Abajo) abajo = l;
                }
            }
        }

        List<BossAttack[]> lista = new List<BossAttack[]>();
        if (lase != null && circ != null) lista.Add(new BossAttack[] { lase, circ });
        if (arriba != null && abajo != null) lista.Add(new BossAttack[] { arriba, abajo });
        if (bal != null && circ != null) lista.Add(new BossAttack[] { bal, circ });

        if (lista.Count == 0) return null;

        int i = Random.Range(0, lista.Count);
        if (lista.Count > 1 && i == ultimoCombo) i = (i + 1) % lista.Count;
        ultimoCombo = i;
        return lista[i];
    }

    // ---------- DISPAROS ----------

    public BossBullet Disparar(Vector2 pos, float anguloGrados, float velocidad, float escala, Color color,
                               float vidaSeg, int dano, float aceleracion = 0f, float velAngular = 0f, EstiloBala estilo = null)
    {
        float mult = MultiplicadorVelocidad;   // más rápidas en fase 2 y 3 (la curva conserva su forma)
        BossBullet b = BossBulletPool.Obtener();
        b.Init(this, pos, anguloGrados, velocidad * mult, escala, color, vidaSeg, dano, aceleracion, velAngular * mult, estilo);
        return b;
    }

    // Anillo de balas. huecoAncho > 0 deja un hueco (en grados) centrado en huecoCentro.
    public void Anillo(Vector2 centro, int cantidad, float anguloBase, float radioInicial, float velocidad,
                       float escala, Color color, float vidaSeg, int dano,
                       float huecoCentro = 0f, float huecoAncho = 0f, float aceleracion = 0f, float velAngular = 0f,
                       EstiloBala estilo = null)
    {
        float paso = 360f / cantidad;
        for (int i = 0; i < cantidad; i++)
        {
            float ang = anguloBase + i * paso;
            if (huecoAncho > 0f && Mathf.Abs(Mathf.DeltaAngle(ang, huecoCentro)) < huecoAncho * 0.5f) continue;

            float rad = ang * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Disparar(centro + d * radioInicial, ang, velocidad, escala, color, vidaSeg, dano, aceleracion, velAngular, estilo);
        }
    }

    // ---------- DAÑO (la invulnerabilidad vive en Vida.cs) ----------

    public bool ImpactaJugador(Vector2 pos, float radio, int dano)
    {
        if (jugador == null) return false;
        float r = radio + radioJugador;
        if (((Vector2)jugador.position - pos).sqrMagnitude > r * r) return false;
        AplicarDano(dano);
        return true;
    }

    public bool ImpactaJugadorSegmento(Vector2 a, Vector2 b, float mitadAncho, int dano)
    {
        if (jugador == null) return false;
        Vector2 p = jugador.position;
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        Vector2 cercano = a + ab * t;
        float r = mitadAncho + radioJugador;
        if ((p - cercano).sqrMagnitude > r * r) return false;
        AplicarDano(dano);
        return true;
    }

    public void AplicarDano(int dano)
    {
        if (vida != null) vida.TakeDamage(dano);
    }

    public bool DentroDeLimite(Vector2 pos)
    {
        return (pos - centroCache).sqrMagnitude < limiteSqr;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        DibujarCirculo(ArenaCentro, ArenaRadio);

        if (jugador != null)
        {
            Gizmos.color = Color.cyan;
            DibujarCirculo(jugador.position, radioJugador);
        }
    }

    static void DibujarCirculo(Vector2 c, float r)
    {
        const int n = 48;
        Vector3 prev = c + new Vector2(r, 0f);
        for (int i = 1; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 next = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
