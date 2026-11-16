using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ATAQUES 4 y 5: marcadores que avisan por dónde caerán proyectiles desde arriba o desde abajo.
// Añade este componente DOS veces al jefe: uno con Origen = Arriba y otro con Origen = Abajo.
public class AtaqueLluvia : BossAttack
{
    public enum Origen { Arriba, Abajo, Alternar }
    public Origen origen = Origen.Arriba;

    [Header("Columnas")]
    public int columnas = 6;
    [Tooltip("Marcas añadidas a 'Columnas'. Total = Columnas + Columnas Extra.")]
    public int columnasExtra = 6;
    public float anchoColumna = 1.2f;
    public float separacionMinima = 0.4f;
    [Tooltip("Una de las columnas siempre cae donde está el jugador.")]
    public bool columnaEnJugador = true;
    public int repeticiones = 3;
    public float pausaEntreRepeticiones = 0.6f;

    [Header("Aviso")]
    public float tiempoAviso = 1.2f;
    public Color colorAviso = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Proyectiles")]
    public float duracionLluvia = 1.4f;
    public float intervalo = 0.07f;
    public float velocidad = 10f;
    public float escala = 0.35f;
    public Color color = new Color(0.4f, 0.8f, 1f, 1f);

    public override IEnumerator Ejecutar(BossController boss)
    {
        for (int r = 0; r < repeticiones; r++)
        {
            bool desdeArriba = origen == Origen.Arriba || (origen == Origen.Alternar && r % 2 == 0);
            Vector2 centro = boss.ArenaCentro;
            float radio = boss.ArenaRadio;

            List<float> xs = ElegirColumnas(boss, centro, radio);
            var datos = new List<(float x, float yBorde)>();

            // --- Avisos ---
            foreach (float x in xs)
            {
                float yBorde = Mathf.Sqrt(Mathf.Max(0.01f, radio * radio - x * x));
                datos.Add((x, yBorde));

                // Franja que cubre toda la columna
                SpriteRenderer franja = BossSprites.Crear("AvisoColumna", BossSprites.Cuadrado,
                    new Vector2(centro.x + x, centro.y), new Vector2(anchoColumna, yBorde * 2f), colorAviso, 10);
                StartCoroutine(Parpadeo(franja, tiempoAviso, colorAviso, 0.12f, 0.35f));

                // Marcador en el extremo por donde entran los proyectiles
                float yMarca = centro.y + (desdeArriba ? yBorde : -yBorde);
                SpriteRenderer marca = BossSprites.Crear("AvisoOrigen", BossSprites.Circulo,
                    new Vector2(centro.x + x, yMarca), new Vector2(anchoColumna * 0.6f, anchoColumna * 0.6f), colorAviso, 11);
                StartCoroutine(Parpadeo(marca, tiempoAviso, colorAviso, 0.3f, 0.9f, 6f));
            }

            yield return new WaitForSeconds(tiempoAviso);

            // --- Lluvia ---
            float t = 0f;
            float angulo = desdeArriba ? -90f : 90f;
            while (t < duracionLluvia)
            {
                foreach (var d in datos)
                {
                    float dx = Random.Range(-anchoColumna * 0.5f + escala * 0.5f, anchoColumna * 0.5f - escala * 0.5f);
                    float y = centro.y + (desdeArriba ? d.yBorde : -d.yBorde);
                    float vida = (d.yBorde * 2f) / velocidad + 0.5f;
                    boss.Disparar(new Vector2(centro.x + d.x + dx, y), angulo, velocidad, escala, color, vida, dano, 0f, 0f, estilo);
                }
                float espera = Intervalo(boss, intervalo);
                t += espera;
                yield return new WaitForSeconds(espera);
            }

            if (r < repeticiones - 1) yield return Esperar(boss, pausaEntreRepeticiones);
        }
    }

    // Devuelve posiciones X relativas al centro de la arena
    List<float> ElegirColumnas(BossController boss, Vector2 centro, float radio)
    {
        var lista = new List<float>();
        float maxX = Mathf.Max(0f, radio - anchoColumna * 0.5f);

        if (columnaEnJugador && boss.jugador != null)
            lista.Add(Mathf.Clamp(boss.jugador.position.x - centro.x, -maxX, maxX));

        int intentos = 0;
        int total = columnas + columnasExtra;
        while (lista.Count < total && intentos < 200)
        {
            intentos++;
            float x = Random.Range(-maxX, maxX);
            bool libre = true;
            foreach (float otra in lista)
            {
                if (Mathf.Abs(x - otra) < anchoColumna + separacionMinima) { libre = false; break; }
            }
            if (libre) lista.Add(x);
        }
        return lista;
    }
}
