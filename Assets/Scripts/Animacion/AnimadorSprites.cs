using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Animador de sprites por código: trocea una hoja PNG en una rejilla uniforme y reproduce
// idle / mover / atacar. No necesita Animator ni clips .anim.
//
// - Si no hay animación de "mover", hace un saltito + estiramiento (efecto procedural).
// - Si no hay "idle", usa el primer frame de "atacar" (así no cambia de pivote al animarse).
// - Si no hay "atacar", hace un golpe de escala.
// Para enemigos/jefe crea un hijo "Visual" que dibuja el sprite (así los efectos no tocan el collider
// ni el Rigidbody). Para proyectiles pon 'usarHijoVisual' = false y anima el propio SpriteRenderer.
public class AnimadorSprites : MonoBehaviour
{
    [System.Serializable]
    public class Clip
    {
        [Tooltip("PNG con todos los frames en una rejilla uniforme.")]
        public Texture2D hoja;
        [Min(1)] public int columnas = 1;
        [Min(1)] public int filas = 1;
        [Tooltip("0 = todos los de la rejilla (columnas x filas).")]
        public int frames = 0;
        public float fps = 12f;
        [Tooltip("Opcional: si lo rellenas, se usan estos sprites en vez de la hoja.")]
        public Sprite[] framesManuales;

        public bool Tiene
        {
            get { return (framesManuales != null && framesManuales.Length > 0) || hoja != null; }
        }
    }

    [Header("Animaciones (hojas de sprites)")]
    public Clip idle = new Clip();
    public Clip mover = new Clip();
    public Clip atacar = new Clip();
    [Tooltip("Pixels por unidad de las hojas (tus PNG usan 32).")]
    public float pixelsPorUnidad = 32f;
    [Tooltip("Momento de la animación de ataque en el que cae el golpe/disparo (0 = al empezar, 1 = al terminar).")]
    [Range(0f, 1f)] public float momentoImpacto = 0.5f;

    [Header("Efectos procedurales (cuando falta la animación)")]
    public bool efectoAlMover = true;
    public float bobAltura = 0.1f;
    public float bobVelocidad = 12f;
    public float estiramiento = 0.06f;
    public bool respirar = true;
    public float respiracion = 0.02f;
    public float golpeEscala = 0.18f;
    public float golpeDuracion = 0.3f;

    [Header("Muerte")]
    public float duracionMuerte = 0.35f;
    [Tooltip("Si hay un BossVida en este objeto, se desvanece al morir el jefe.")]
    public bool morirConBossVida = true;

    [Header("Avanzado")]
    [Tooltip("Dibuja en un hijo 'Visual' (enemigos y jefe). Desactívalo en proyectiles.")]
    public bool usarHijoVisual = true;

    SpriteRenderer raiz, vis;
    Transform visT;
    Sprite[] sIdle, sMover, sAtacar;
    bool hayMover, hayAtaque;
    bool moviendo, atacando, muerto;
    float tiempo, tAtaque, fase, alphaMuerte = 1f, escalaMuerte = 1f;
    BossVida bossVida;

    public bool Atacando { get { return atacando; } }
    public bool Muerto { get { return muerto; } }
    public bool TieneClipAtacar { get { return hayAtaque; } }

    public float DuracionAtaque
    {
        get { return hayAtaque ? sAtacar.Length / Mathf.Max(1f, atacar.fps) : golpeDuracion; }
    }

    // Segundos desde que empieza el ataque hasta el momento del impacto
    public float RetardoImpacto { get { return DuracionAtaque * momentoImpacto; } }

    // ---------- Caché de sprites ----------

    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimpiarCache()
    {
        cache.Clear();
    }

    // Trocea 'hoja' en frames (la fila 0 es la de ARRIBA, como se lee la imagen).
    public static Sprite[] ObtenerFrames(Texture2D hoja, int columnas, int filas, int frames, float ppu, Vector2 pivote)
    {
        if (hoja == null) return null;

        columnas = Mathf.Max(1, columnas);
        filas = Mathf.Max(1, filas);
        int total = columnas * filas;
        if (frames <= 0 || frames > total) frames = total;

        string clave = hoja.name + "_" + hoja.width + "x" + hoja.height + "_" + columnas + "_" + filas + "_" + frames + "_" + ppu + "_" + pivote.x + "_" + pivote.y;
        Sprite[] lista;
        if (cache.TryGetValue(clave, out lista) && lista != null && lista.Length > 0 && lista[0] != null) return lista;

        int w = hoja.width / columnas;
        int h = hoja.height / filas;
        lista = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            int c = i % columnas;
            int f = i / columnas;
            Rect r = new Rect(c * w, hoja.height - (f + 1) * h, w, h);
            lista[i] = Sprite.Create(hoja, r, pivote, ppu, 0, SpriteMeshType.FullRect);
            lista[i].name = hoja.name + "_" + i;
        }

        cache[clave] = lista;
        return lista;
    }

    public static Sprite[] ObtenerFrames(Texture2D hoja, int columnas, int filas, int frames, float ppu)
    {
        return ObtenerFrames(hoja, columnas, filas, frames, ppu, new Vector2(0.5f, 0.5f));
    }

    Sprite[] Obtener(Clip c)
    {
        if (c == null) return null;
        if (c.framesManuales != null && c.framesManuales.Length > 0) return c.framesManuales;
        return ObtenerFrames(c.hoja, c.columnas, c.filas, c.frames, pixelsPorUnidad);
    }

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        raiz = GetComponent<SpriteRenderer>();
        if (raiz == null)
        {
            Debug.LogWarning("AnimadorSprites necesita un SpriteRenderer en " + name, this);
            enabled = false;
            return;
        }

        sIdle = Obtener(idle);
        sMover = Obtener(mover);
        sAtacar = Obtener(atacar);

        hayAtaque = sAtacar != null && sAtacar.Length > 0;
        hayMover = sMover != null && sMover.Length > 0;

        if ((sIdle == null || sIdle.Length == 0) && hayAtaque) sIdle = new Sprite[] { sAtacar[0] };

        if (usarHijoVisual) CrearVisual();
        else vis = raiz;

        if (morirConBossVida)
        {
            bossVida = GetComponent<BossVida>();
            if (bossVida != null) bossVida.HaMuerto += AlMorirJefe;
        }
    }

    void OnDestroy()
    {
        if (bossVida != null) bossVida.HaMuerto -= AlMorirJefe;
    }

    void AlMorirJefe()
    {
        Morir();
    }

    void CrearVisual()
    {
        GameObject go = new GameObject("Visual");
        go.transform.SetParent(transform, false);

        vis = go.AddComponent<SpriteRenderer>();
        vis.sprite = raiz.sprite;
        vis.sharedMaterial = raiz.sharedMaterial;
        vis.sortingLayerID = raiz.sortingLayerID;
        vis.sortingOrder = raiz.sortingOrder;
        vis.maskInteraction = raiz.maskInteraction;
        vis.flipX = raiz.flipX;
        vis.flipY = raiz.flipY;
        vis.color = raiz.color;
        visT = go.transform;

        raiz.enabled = false;
    }

    // ---------- API ----------

    public void SetMoviendo(bool valor)
    {
        moviendo = valor;
    }

    public void Atacar()
    {
        if (muerto || atacando) return;
        atacando = true;
        tAtaque = 0f;
    }

    // Devuelve los segundos que dura el desvanecimiento (para destruir el objeto después)
    public float Morir()
    {
        if (muerto) return 0f;
        muerto = true;
        atacando = false;
        StartCoroutine(AnimarMuerte());
        return duracionMuerte;
    }

    IEnumerator AnimarMuerte()
    {
        float t = 0f;
        while (t < duracionMuerte)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duracionMuerte));
            alphaMuerte = 1f - k;
            escalaMuerte = Mathf.Lerp(1f, 0.6f, k);
            yield return null;
        }
        alphaMuerte = 0f;
    }

    // ---------- Animación ----------

    void LateUpdate()
    {
        if (vis == null) return;

        float dt = Time.deltaTime;
        tiempo += dt;
        Sprite sprite = null;

        if (atacando)
        {
            tAtaque += dt;
            if (hayAtaque)
            {
                int i = Mathf.FloorToInt(tAtaque * Mathf.Max(1f, atacar.fps));
                if (i >= sAtacar.Length) atacando = false;
                else sprite = sAtacar[i];
            }
            else if (tAtaque >= golpeDuracion)
            {
                atacando = false;
            }
        }

        if (!atacando && !muerto)
        {
            bool usaMover = moviendo && hayMover;
            Sprite[] f = usaMover ? sMover : sIdle;
            float fps = usaMover ? mover.fps : idle.fps;
            if (f != null && f.Length > 0)
                sprite = f[Mathf.FloorToInt(tiempo * Mathf.Max(1f, fps)) % f.Length];
        }

        if (sprite != null && vis.sprite != sprite) vis.sprite = sprite;

        if (visT == null) return;

        // Copiamos lo que otros scripts hacen sobre el SpriteRenderer original (flip y destellos de color)
        vis.flipX = raiz.flipX;
        Color c = raiz.color;
        c.a *= alphaMuerte;
        vis.color = c;

        Vector3 pos = Vector3.zero;
        Vector3 esc = Vector3.one;

        if (!muerto)
        {
            if (moviendo && efectoAlMover && !hayMover)
            {
                fase += dt * bobVelocidad;
                pos.y = Mathf.Abs(Mathf.Sin(fase)) * bobAltura;
                float k = Mathf.Sin(fase * 2f) * estiramiento;
                esc = new Vector3(1f - k, 1f + k, 1f);
            }
            else if (!moviendo && !atacando && respirar)
            {
                float k = Mathf.Sin(tiempo * 2.5f) * respiracion;
                esc = new Vector3(1f - k, 1f + k, 1f);
            }

            if (atacando && !hayAtaque)
            {
                float p = Mathf.Clamp01(tAtaque / Mathf.Max(0.01f, golpeDuracion));
                esc *= 1f + Mathf.Sin(p * Mathf.PI) * golpeEscala;
            }
        }

        esc *= escalaMuerte;
        visT.localPosition = pos;
        visT.localScale = esc;
    }
}
