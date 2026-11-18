using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ATAQUE ESPECIAL (se desbloquea en la fase 3): una rejilla de líneas avisa por dónde van a pasar los
// disparos. Las líneas forman cuadrados o rombos y cubren toda la arena; el hueco seguro está DENTRO de
// cada casilla. Cada ronda usa un patrón distinto (cuadrados, rombos, rombos anchos...).
public class AtaqueRejilla : BossAttack
{
    [System.Serializable]
    public class Patron
    {
        public string nombre = "Cuadrados";
        [Tooltip("Ángulo (grados) de la primera familia de líneas.")]
        public float angulo = 0f;
        [Tooltip("Ángulo entre las dos familias: 90 = cuadrados, otro valor = rombos.")]
        public float anguloEntreFamilias = 90f;
    }

    [Header("Rondas")]
    public int rondas = 3;
    public float pausaEntreRondas = 0.8f;
    public Patron[] patrones = new Patron[]
    {
        new Patron { nombre = "Cuadrados", angulo = 10f, anguloEntreFamilias = 90f },
        new Patron { nombre = "Rombos", angulo = 45f, anguloEntreFamilias = 90f },
        new Patron { nombre = "Rombos anchos", angulo = 20f, anguloEntreFamilias = 60f },
    };
    [Tooltip("Giro aleatorio extra (±grados) para que cada ronda sea distinta.")]
    public float variacionAngulo = 8f;

    [Header("Líneas de aviso")]
    [Tooltip("Distancia entre líneas paralelas = tamaño de cada casilla.")]
    public float separacion = 3.5f;
    public float grosorAviso = 0.12f;
    public float tiempoAviso = 1.4f;
    public Color colorFamiliaA = new Color(1f, 0.55f, 0.15f, 1f);
    public Color colorFamiliaB = new Color(0.85f, 0.2f, 0.9f, 1f);

    [Header("Disparos")]
    public float velocidad = 13f;
    [Tooltip("Distancia entre balas consecutivas de una misma línea.")]
    public float espaciado = 1.4f;
    public float duracionDisparo = 1.3f;
    [Tooltip("Tamaño de la hitbox de cada bala.")]
    public float escala = 0.5f;
    public float vidaBala = 6f;

    class Linea
    {
        public Vector2 inicio, fin;
        public float angulo;
        public int familia;
        public bool invertida;
    }

    public AtaqueRejilla()
    {
        faseMinima = 3;
    }

    public override IEnumerator Ejecutar(BossController boss)
    {
        if (patrones == null || patrones.Length == 0) yield break;

        int primero = Random.Range(0, patrones.Length);

        for (int r = 0; r < rondas; r++)
        {
            Patron p = patrones[(primero + r) % patrones.Length];
            float baseAng = p.angulo + Random.Range(-variacionAngulo, variacionAngulo);
            float desfase = Random.Range(0f, separacion);

            List<Linea> lineas = new List<Linea>();
            CrearFamilia(boss, lineas, baseAng, desfase, 0, colorFamiliaA);
            CrearFamilia(boss, lineas, baseAng + p.anguloEntreFamilias, desfase * 0.5f, 1, colorFamiliaB);

            yield return new WaitForSeconds(tiempoAviso);          // el aviso no se acorta: hay que poder reaccionar
            yield return StartCoroutine(Disparar(boss, lineas));

            if (r < rondas - 1) yield return Esperar(boss, pausaEntreRondas);
        }
    }

    // Dibuja el aviso de una familia de líneas paralelas y las apunta en la lista
    void CrearFamilia(BossController boss, List<Linea> lineas, float anguloGrados, float desfase, int familia, Color color)
    {
        Vector2 centro = boss.ArenaCentro;
        float radio = boss.ArenaRadio;

        float rad = anguloGrados * Mathf.Deg2Rad;
        Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 normal = new Vector2(-d.y, d.x);

        int n = Mathf.CeilToInt(radio / separacion);
        for (int k = -n; k <= n; k++)
        {
            float off = k * separacion + desfase;
            if (Mathf.Abs(off) >= radio - 0.5f) continue;

            float h = Mathf.Sqrt(radio * radio - off * off);       // media longitud de la cuerda
            Vector2 mitad = centro + normal * off;

            Linea l = new Linea();
            l.inicio = mitad - d * h;
            l.fin = mitad + d * h;
            l.angulo = anguloGrados;
            l.familia = familia;
            l.invertida = ((k + familia) & 1) != 0;                 // alternan el sentido: efecto "tejido"
            lineas.Add(l);

            SpriteRenderer aviso = BossSprites.Crear("AvisoRejilla", BossSprites.Cuadrado, mitad,
                                                     new Vector2(h * 2f, grosorAviso), color, 12);
            aviso.transform.rotation = Quaternion.Euler(0f, 0f, anguloGrados);
            StartCoroutine(Parpadeo(aviso, tiempoAviso, color, 0.15f, 0.7f, 5f));
        }
    }

    IEnumerator Disparar(BossController boss, List<Linea> lineas)
    {
        EstiloBala estiloA = estilo != null && estilo.Valido ? estilo.ConTinte(colorFamiliaA) : null;
        EstiloBala estiloB = estilo != null && estilo.Valido ? estilo.ConTinte(colorFamiliaB) : null;

        float vReal = Mathf.Max(0.5f, velocidad * boss.MultiplicadorVelocidad);
        float intervalo = espaciado / vReal;                         // así la separación entre balas se mantiene en cualquier fase

        float t = 0f;
        while (t < duracionDisparo)
        {
            foreach (Linea l in lineas)
            {
                Vector2 desde = l.invertida ? l.fin : l.inicio;
                float ang = l.invertida ? l.angulo + 180f : l.angulo;
                Color c = l.familia == 0 ? colorFamiliaA : colorFamiliaB;
                boss.Disparar(desde, ang, velocidad, escala, c, vidaBala, dano, 0f, 0f, l.familia == 0 ? estiloA : estiloB);
            }

            t += intervalo;
            yield return new WaitForSeconds(intervalo);
        }
    }
}
