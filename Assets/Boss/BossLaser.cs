using System.Collections;
using UnityEngine;

// Láser con fase de aviso (línea fina parpadeante) y fase activa (rayo grueso que hace daño).
// Se crea desde AtaqueLaseres con BossLaser.Crear(...) y se autodestruye al terminar.
// Si el ataque tiene un EstiloBala con hoja (p. ej. laserjefe_128x32_128x32_8f), el rayo activo se dibuja con esa animación.
public class BossLaser : MonoBehaviour
{
    BossController boss;
    Transform origen;
    Vector2 posFija;
    float angulo, distanciaInicio, longitud, tAviso, tActivo, anchoAviso, anchoLaser, velRot;
    int dano;
    Color cAviso, cLaser;
    SpriteRenderer sr, nucleo;

    Sprite[] framesRayo;
    float fpsRayo, anchoSprite = 1f, altoSprite = 1f, fraccionVisible = 1f;

    public static BossLaser Crear(BossController boss, Transform origen, float angulo, float distanciaInicio,
                                  float longitud, float tAviso, float tActivo, float anchoAviso, float anchoLaser,
                                  float velRot, int dano, Color cAviso, Color cLaser, EstiloBala estilo = null)
    {
        var go = new GameObject("BossLaser");
        go.transform.SetParent(BossSprites.Raiz, true);
        var l = go.AddComponent<BossLaser>();

        l.boss = boss;
        l.origen = origen;
        l.posFija = origen != null ? (Vector2)origen.position : (Vector2)boss.transform.position;
        l.angulo = angulo;
        l.distanciaInicio = distanciaInicio;
        l.longitud = longitud;
        l.tAviso = tAviso;
        l.tActivo = tActivo;
        l.anchoAviso = anchoAviso;
        l.anchoLaser = anchoLaser;
        l.velRot = velRot;
        l.dano = dano;
        l.cAviso = cAviso;
        l.cLaser = cLaser;

        if (estilo != null && estilo.Valido)
        {
            // El pivote va en el centro del lado izquierdo para que el rayo nazca en el origen
            l.framesRayo = AnimadorSprites.ObtenerFrames(estilo.hoja, estilo.columnas, estilo.filas, estilo.frames,
                                                         estilo.pixelsPorUnidad, new Vector2(0f, 0.5f));
            l.fpsRayo = estilo.fps;
            l.fraccionVisible = Mathf.Max(0.05f, estilo.fraccionVisible);
            if (l.framesRayo != null && l.framesRayo.Length > 0)
            {
                l.anchoSprite = Mathf.Max(0.0001f, l.framesRayo[0].bounds.size.x);
                l.altoSprite = Mathf.Max(0.0001f, l.framesRayo[0].bounds.size.y);
            }
        }

        l.sr = go.AddComponent<SpriteRenderer>();
        l.sr.sprite = BossSprites.CuadradoIzquierda;
        l.sr.sortingOrder = 50;
        l.sr.color = cAviso;

        var n = new GameObject("Nucleo");
        n.transform.SetParent(go.transform, false);
        n.transform.localScale = new Vector3(1f, 0.4f, 1f);
        l.nucleo = n.AddComponent<SpriteRenderer>();
        l.nucleo.sprite = BossSprites.CuadradoIzquierda;
        l.nucleo.sortingOrder = 51;
        l.nucleo.color = Color.white;
        l.nucleo.enabled = false;

        Vector2 a, b;
        l.Colocar(anchoAviso, 1f, 1f, out a, out b);
        return l;
    }

    void Start()
    {
        StartCoroutine(Rutina());
    }

    // Posiciona el láser y devuelve sus extremos. anchoSpr/altoSpr: tamaño del sprite en unidades.
    void Colocar(float ancho, float anchoSpr, float altoSpr, out Vector2 a, out Vector2 b)
    {
        Vector2 o = origen != null ? (Vector2)origen.position : posFija;
        float rad = angulo * Mathf.Deg2Rad;
        Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        a = o + d * distanciaInicio;
        b = a + d * longitud;

        transform.SetPositionAndRotation(a, Quaternion.Euler(0f, 0f, angulo));
        transform.localScale = new Vector3(longitud / anchoSpr, ancho / altoSpr, 1f);
    }

    IEnumerator Rutina()
    {
        Vector2 a, b;

        // ---- AVISO ----
        float t = 0f;
        while (t < tAviso)
        {
            t += Time.deltaTime;
            angulo += velRot * Time.deltaTime;
            Colocar(anchoAviso, 1f, 1f, out a, out b);

            float progreso = Mathf.Clamp01(t / tAviso);
            float parpadeo = 0.5f + 0.5f * Mathf.Sin(t * 20f);
            Color c = cAviso;
            c.a = Mathf.Lerp(0.15f, 0.75f, parpadeo) * Mathf.Lerp(0.5f, 1f, progreso);
            sr.color = c;
            yield return null;
        }

        // ---- ACTIVO ----
        bool conSprite = framesRayo != null && framesRayo.Length > 0;
        if (conSprite)
        {
            sr.sprite = framesRayo[0];
            sr.color = Color.white;
        }
        else
        {
            sr.color = cLaser;
            nucleo.enabled = true;
        }

        t = 0f;
        while (t < tActivo)
        {
            t += Time.deltaTime;
            angulo += velRot * Time.deltaTime;

            float env = Mathf.Min(Mathf.Clamp01(t / 0.12f), Mathf.Clamp01((tActivo - t) / 0.25f));
            float ancho = anchoLaser * env;

            if (conSprite)
            {
                sr.sprite = framesRayo[(int)(t * fpsRayo) % framesRayo.Length];
                Colocar(ancho / fraccionVisible, anchoSprite, altoSprite, out a, out b);
            }
            else
            {
                Colocar(ancho, 1f, 1f, out a, out b);
            }

            if (env > 0.5f) boss.ImpactaJugadorSegmento(a, b, ancho * 0.5f * 0.9f, dano);
            yield return null;
        }

        Destroy(gameObject);
    }
}
