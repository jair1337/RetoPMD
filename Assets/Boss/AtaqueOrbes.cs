using System.Collections;
using UnityEngine;

// ATAQUE ESPECIAL (se desbloquea en la fase 4): aparecen orbes en puntos de la arena y, tras una mecha
// de aviso, cada uno explota en un CÍRCULO de balas pequeñas equidistantes que se expande desde el centro
// del orbe (sin dispersión aleatoria). El orbe también hace daño si lo tocas mientras espera.
public class AtaqueOrbes : BossAttack
{
    [Header("Olas")]
    public int olas = 3;
    public int orbesPorOla = 5;
    public float retardoEntreOrbes = 0.15f;
    public float pausaEntreOlas = 0.9f;

    [Header("Orbe")]
    [Tooltip("Segundos desde que aparece hasta que explota.")]
    public float tiempoMecha = 1.3f;
    public float escalaOrbe = 1.1f;
    public Color colorOrbe = new Color(0.85f, 0.6f, 1f, 1f);
    public Color colorAviso = new Color(1f, 0.2f, 0.2f, 1f);
    [Tooltip("Sprite del orbe (hoja animada). Vacío = usa el de las balas de este ataque.")]
    public EstiloBala estiloOrbe = new EstiloBala();

    [Header("Explosión (círculo que se expande)")]
    [Tooltip("Balas de cada círculo, repartidas a la misma distancia. Más balas = círculo más denso.")]
    public int balasPorCirculo = 36;
    [Tooltip("Círculos seguidos por explosión (el 2.º va intercalado entre las balas del 1.º).")]
    public int circulosPorExplosion = 2;
    public float retardoEntreCirculos = 0.12f;
    public float velocidad = 5f;
    public float escalaBala = 0.3f;
    public Color colorBala = new Color(1f, 0.75f, 0.75f, 1f);
    public float vidaBala = 8f;

    [Header("Colocación")]
    [Tooltip("Marcado: todos los orbes aparecen en el centro de la arena y sueltan círculos que se expanden (efecto de ondas). Desmarcado: puntos aleatorios.")]
    public bool aparecerEnCentro = false;
    [Range(0.3f, 1f)] public float fraccionArena = 0.85f;
    [Tooltip("Los orbes no aparecen más cerca que esto del jugador.")]
    public float distanciaMinimaJugador = 3f;

    public AtaqueOrbes()
    {
        faseMinima = 4;
    }

    public override IEnumerator Ejecutar(BossController boss)
    {
        for (int o = 0; o < olas; o++)
        {
            for (int i = 0; i < orbesPorOla; i++)
            {
                StartCoroutine(Orbe(boss, ElegirPunto(boss)));
                yield return Esperar(boss, retardoEntreOrbes);
            }
            yield return Esperar(boss, pausaEntreOlas);
        }

        // Espera a que exploten los últimos
        yield return new WaitForSeconds(tiempoMecha + circulosPorExplosion * retardoEntreCirculos + 0.4f);
    }

    Vector2 ElegirPunto(BossController boss)
    {
        if (aparecerEnCentro) return boss.ArenaCentro;

        Vector2 p = PuntoEnArena(boss, fraccionArena);
        if (boss.jugador == null) return p;

        for (int i = 0; i < 20; i++)
        {
            if (Vector2.Distance(p, boss.jugador.position) >= distanciaMinimaJugador) break;
            p = PuntoEnArena(boss, fraccionArena);
        }
        return p;
    }

    IEnumerator Orbe(BossController boss, Vector2 pos)
    {
        EstiloBala estiloDelOrbe = estiloOrbe != null && estiloOrbe.Valido ? estiloOrbe : estilo;

        // Orbe quieto que dura lo que la mecha
        boss.Disparar(pos, 0f, 0f, escalaOrbe, colorOrbe, tiempoMecha, dano, 0f, 0f, estiloDelOrbe);

        // Círculo rojo parpadeante bajo el orbe
        float d = escalaOrbe * 2.4f;
        SpriteRenderer aviso = BossSprites.Crear("AvisoOrbe", BossSprites.Circulo, pos, new Vector2(d, d), colorAviso, 11);
        StartCoroutine(Parpadeo(aviso, tiempoMecha, colorAviso, 0.1f, 0.45f, 6f));

        yield return new WaitForSeconds(tiempoMecha);

        // Círculo perfecto: balas equidistantes, misma velocidad y sin hueco ni giro
        int circulos = Mathf.Max(1, circulosPorExplosion);
        int cantidad = Mathf.Max(3, balasPorCirculo);
        float paso = 360f / cantidad;
        for (int a = 0; a < circulos; a++)
        {
            float baseAng = (a & 1) * paso * 0.5f;                  // los círculos pares quedan intercalados con los impares
            boss.Anillo(pos, cantidad, baseAng, 0.2f, velocidad, escalaBala, colorBala, vidaBala, dano,
                        0f, 0f, 0f, 0f, estilo);
            if (a < circulos - 1) yield return Esperar(boss, retardoEntreCirculos);
        }
    }
}
