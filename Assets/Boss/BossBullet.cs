using System.Collections.Generic;
using UnityEngine;

// Bala del jefe. No usa Rigidbody ni colliders: se mueve a mano y comprueba la
// distancia al jugador (mucho más barato con cientos de balas en pantalla).
// El sprite viene del EstiloBala del ataque (hoja animada) o de BossController.spriteBala.
// Los sprites deben apuntar hacia +X (derecha).
[RequireComponent(typeof(SpriteRenderer))]
public class BossBullet : MonoBehaviour
{
    SpriteRenderer sr;
    BossController boss;
    Vector2 dir;
    float speed, accel, angVel, lifetime, age, hitRadius;
    int damage;
    bool enPool;

    Sprite[] frames;
    float fps, desfase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(BossController boss, Vector2 pos, float anguloGrados, float velocidad, float escala,
                     Color color, float vidaSeg, int dano, float aceleracion, float velAngular, EstiloBala estilo)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        this.boss = boss;
        speed = velocidad;
        accel = aceleracion;
        angVel = velAngular;
        lifetime = vidaSeg;
        damage = dano;
        age = 0f;
        hitRadius = escala * 0.5f * boss.factorHitboxBala;

        float rad = anguloGrados * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float visible = 1f;
        Color tinte = color;
        frames = null;

        if (estilo != null && estilo.Valido)
        {
            frames = AnimadorSprites.ObtenerFrames(estilo.hoja, estilo.columnas, estilo.filas, estilo.frames, estilo.pixelsPorUnidad);
            fps = estilo.fps;
            visible = Mathf.Max(0.05f, estilo.fraccionVisible);
            tinte = estilo.tinte;
            desfase = Random.value * 10f;
        }

        Sprite spr;
        if (frames != null && frames.Length > 0) spr = frames[0];
        else if (boss.spriteBala != null) { spr = boss.spriteBala; tinte = Color.white; }
        else spr = BossSprites.Circulo;

        // 'escala' = diámetro visible en unidades de mundo, sea cual sea el tamaño del sprite
        float esc = escala / (Mathf.Max(0.0001f, spr.bounds.size.x) * visible);

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, 0f, anguloGrados);
        transform.localScale = new Vector3(esc, esc, 1f);

        sr.sprite = spr;
        sr.color = tinte;
        sr.sortingOrder = 100;

        enPool = false;
        BossBulletPool.Registrar(this);
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (boss == null) { Reciclar(); return; }

        float dt = Time.deltaTime;
        age += dt;
        if (age >= lifetime) { Reciclar(); return; }

        if (frames != null && frames.Length > 1)
            sr.sprite = frames[(int)(age * fps + desfase) % frames.Length];

        if (angVel != 0f)
        {
            float giro = angVel * dt * Mathf.Deg2Rad;
            float c = Mathf.Cos(giro), s = Mathf.Sin(giro);
            dir = new Vector2(dir.x * c - dir.y * s, dir.x * s + dir.y * c);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        if (accel != 0f) speed = Mathf.Max(0f, speed + accel * dt);

        Vector2 pos = (Vector2)transform.position + dir * speed * dt;
        transform.position = pos;

        if (boss.ImpactaJugador(pos, hitRadius, damage)) { Reciclar(); return; }
        if (!boss.DentroDeLimite(pos)) Reciclar();
    }

    public void Reciclar()
    {
        if (enPool) return;
        enPool = true;
        BossBulletPool.Quitar(this);
        gameObject.SetActive(false);
        BossBulletPool.Devolver(this);
    }
}

public static class BossBulletPool
{
    static readonly Stack<BossBullet> libres = new Stack<BossBullet>();
    static readonly HashSet<BossBullet> activas = new HashSet<BossBullet>();
    static Transform raiz;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Reiniciar();
    }

    public static void Reiniciar()
    {
        libres.Clear();
        activas.Clear();
        raiz = null;
    }

    public static BossBullet Obtener()
    {
        while (libres.Count > 0)
        {
            BossBullet b = libres.Pop();
            if (b != null) return b;
        }

        if (raiz == null) raiz = new GameObject("BossBullets").transform;

        var go = new GameObject("BossBullet");
        go.transform.SetParent(raiz, false);
        go.AddComponent<SpriteRenderer>();
        return go.AddComponent<BossBullet>();
    }

    public static void Devolver(BossBullet b) { libres.Push(b); }
    public static void Registrar(BossBullet b) { activas.Add(b); }
    public static void Quitar(BossBullet b) { activas.Remove(b); }

    // Quita todas las balas que están en pantalla
    public static void LimpiarActivas()
    {
        var copia = new List<BossBullet>(activas);
        foreach (BossBullet b in copia)
            if (b != null) b.Reciclar();
        activas.Clear();
    }
}
