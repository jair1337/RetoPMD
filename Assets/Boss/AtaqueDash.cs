using System.Collections;
using UnityEngine;

// ATAQUE ESPECIAL (se desbloquea en la fase 4): el jefe sale del centro hacia el borde de la arena, avisa con
// una línea roja y se lanza en línea recta hacia donde estás, pasando de largo. Hace daño al tocarte y deja
// una estela de balas. Tras varios dashes vuelve a su sitio.
public class AtaqueDash : BossAttack
{
    [Header("Dash")]
    public int dashes = 3;
    [Tooltip("Tiempo que se ve la línea roja antes de lanzarse.")]
    public float tiempoAviso = 0.9f;
    public float velocidadDash = 28f;
    [Tooltip("Cuánto pasa de largo del punto donde estabas.")]
    public float distanciaExtra = 8f;
    [Tooltip("Radio de contacto del jefe (daño al tocarte durante el dash).")]
    public float radioContacto = 1.1f;
    public float pausaTrasDash = 0.35f;

    [Header("Movimiento")]
    public float velocidadReposicion = 22f;
    public float velocidadRegreso = 14f;
    [Tooltip("Dónde se coloca para empezar, como fracción del radio de la arena.")]
    [Range(0.3f, 1f)] public float fraccionBorde = 0.8f;

    [Header("Aviso")]
    public float grosorAviso = 0.25f;
    public Color colorAviso = new Color(1f, 0.15f, 0.1f, 1f);

    [Header("Estela")]
    public bool dejarEstela = true;
    public float intervaloEstela = 0.05f;
    public float escalaEstela = 0.45f;
    public float vidaEstela = 0.9f;
    public Color colorEstela = new Color(1f, 0.55f, 0.25f, 1f);

    public AtaqueDash()
    {
        faseMinima = 4;
    }

    public override IEnumerator Ejecutar(BossController boss)
    {
        Transform t = boss.transform;
        Vector2 centro = boss.ArenaCentro;
        float radio = boss.ArenaRadio;

        // 1) Sale del centro: se coloca en el borde, en el lado contrario al jugador
        Vector2 haciaJugador = PosicionJugador(boss, centro) - centro;
        float angJugador = haciaJugador.sqrMagnitude < 0.01f ? 0f : Mathf.Atan2(haciaJugador.y, haciaJugador.x);
        float ang = angJugador + Mathf.PI + Random.Range(-0.5f, 0.5f);
        Vector2 inicio = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (radio * fraccionBorde);
        yield return StartCoroutine(Mover(boss, inicio, velocidadReposicion, false));

        for (int i = 0; i < dashes; i++)
        {
            // 2) Apunta a donde estás ahora y avisa
            Vector2 origen = t.position;
            Vector2 objetivo = PosicionJugador(boss, centro);
            Vector2 dir = objetivo - origen;
            if (dir.sqrMagnitude < 0.01f) dir = centro - origen;
            dir = dir.normalized;

            float largo = Mathf.Min(Vector2.Distance(origen, objetivo) + distanciaExtra,
                                    DistanciaHastaBorde(origen, dir, centro, radio * 0.97f));
            float angDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            SpriteRenderer aviso = BossSprites.Crear("AvisoDash", BossSprites.CuadradoIzquierda, origen,
                                                     new Vector2(largo, grosorAviso), colorAviso, 12);
            aviso.transform.rotation = Quaternion.Euler(0f, 0f, angDeg);
            StartCoroutine(Parpadeo(aviso, tiempoAviso, colorAviso, 0.2f, 0.8f, 6f));

            yield return new WaitForSeconds(tiempoAviso);

            // 3) Dash en línea recta
            yield return StartCoroutine(Mover(boss, origen + dir * largo, velocidadDash * boss.MultiplicadorVelocidad, true));
            yield return Esperar(boss, pausaTrasDash);
        }

        // 4) Vuelve a su sitio
        yield return StartCoroutine(Mover(boss, boss.PosicionBase, velocidadRegreso, false));
    }

    Vector2 PosicionJugador(BossController boss, Vector2 porDefecto)
    {
        return boss.jugador != null ? (Vector2)boss.jugador.position : porDefecto;
    }

    // Distancia desde 'origen', avanzando en 'dir', hasta salir del círculo de la arena
    static float DistanciaHastaBorde(Vector2 origen, Vector2 dir, Vector2 centro, float radio)
    {
        Vector2 oc = origen - centro;
        float b = Vector2.Dot(oc, dir);
        float c = oc.sqrMagnitude - radio * radio;
        float disc = b * b - c;
        if (disc < 0f) return 0f;
        return Mathf.Max(0f, -b + Mathf.Sqrt(disc));
    }

    IEnumerator Mover(BossController boss, Vector2 destino, float velocidad, bool conDano)
    {
        Transform t = boss.transform;
        float acumulado = 0f;

        while (true)
        {
            Vector2 pos = t.position;
            Vector2 resto = destino - pos;
            float dist = resto.magnitude;
            if (dist <= 0.02f) break;

            float paso = Mathf.Min(dist, velocidad * Time.deltaTime);
            pos = pos + resto.normalized * paso;
            t.position = new Vector3(pos.x, pos.y, t.position.z);

            if (conDano)
            {
                boss.ImpactaJugador(pos, radioContacto, dano);

                if (dejarEstela)
                {
                    acumulado += Time.deltaTime;
                    if (acumulado >= intervaloEstela)
                    {
                        acumulado = 0f;
                        boss.Disparar(pos, 0f, 0f, escalaEstela, colorEstela, vidaEstela, dano, 0f, 0f, estilo);
                    }
                }
            }
            yield return null;
        }
    }
}
