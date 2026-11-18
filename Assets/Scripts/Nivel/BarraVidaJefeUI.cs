using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida del jefe dentro de tu canvas (UIJugador). Se construye sola, empieza oculta y aparece
// cuando EncuentroJefe activa al jefe.
// - Sin sprites: barra horizontal arriba, de rectángulos de color.
// - Con 'Sprite Marco' (p. ej. JefeBarra-removebg-preview): barra VERTICAL en el borde de la pantalla,
//   con el relleno dentro del tubo del marco.
public class BarraVidaJefeUI : MonoBehaviour
{
    [Header("Referencias (se buscan solas)")]
    public BossVida vida;
    public Canvas canvas;

    [Header("Barra horizontal (sin sprite de marco)")]
    public Vector2 tamano = new Vector2(560f, 26f);
    public float margenSuperior = 36f;

    [Header("Barra vertical con marco (sprite de la barra del jefe)")]
    [Tooltip("Desactivado = barra horizontal sencilla (la de antes). Actívalo para usar el marco vertical.")]
    public bool usarMarco = false;
    public Sprite spriteMarco;
    public float alturaMarco = 560f;
    [Tooltip("Ancla en pantalla: (1, 0.5) = borde derecho, centrado verticalmente.")]
    public Vector2 anclaje = new Vector2(1f, 0.5f);
    public Vector2 margenVertical = new Vector2(-30f, 0f);
    [Tooltip("Hueco del tubo dentro de la TEXTURA original, en píxeles: x, y (desde arriba), ancho, alto. Los valores por defecto son de JefeBarra-removebg-preview.")]
    public Rect interiorEnTextura = new Rect(155f, 51f, 56f, 406f);

    [Header("Nombre")]
    public bool mostrarNombre = true;
    public string nombre = "JEFE";

    [Header("Aspecto")]
    public Sprite spriteRelleno;
    public Color colorMarco = new Color(0.05f, 0.03f, 0.08f, 0.85f);
    public Color colorFase1 = new Color(0.8f, 0.15f, 0.2f, 1f);
    public Color colorFase2 = new Color(0.95f, 0.5f, 0.1f, 1f);
    public Color colorFase3 = new Color(0.65f, 0.2f, 0.9f, 1f);
    public Color colorFase4 = new Color(0.95f, 0.1f, 0.35f, 1f);
    public Color colorRetraso = new Color(1f, 1f, 1f, 0.6f);
    public float velocidadRetraso = 0.5f;
    public float duracionAparicion = 0.6f;

    GameObject raiz;
    CanvasGroup grupo;
    Image relleno, retraso;
    float pctRetraso = 1f;

    void Start()
    {
        if (vida == null) vida = GetComponent<BossVida>();
        if (vida == null) { enabled = false; return; }

        if (canvas == null) canvas = CanvasUtil.Buscar();
        if (canvas == null)
        {
            Debug.LogWarning("BarraVidaJefeUI: no hay ningún Canvas en la escena.", this);
            enabled = false;
            return;
        }

        if (usarMarco && spriteMarco != null) ConstruirVertical();
        else ConstruirHorizontal();

        vida.Activada += Mostrar;
        vida.HaMuerto += AlMorir;
    }

    void OnDestroy()
    {
        if (vida != null)
        {
            vida.Activada -= Mostrar;
            vida.HaMuerto -= AlMorir;
        }
        if (raiz != null) Destroy(raiz);
    }

    // ---------- Construcción ----------

    void ConstruirHorizontal()
    {
        raiz = new GameObject("BarraVidaJefe", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rt = (RectTransform)raiz.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -margenSuperior);
        rt.sizeDelta = tamano;

        grupo = raiz.GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        Image marco = raiz.AddComponent<Image>();
        marco.color = colorMarco;
        marco.raycastTarget = false;

        Vector2 margen = new Vector2(4f, 4f);
        retraso = CrearBarra("Retraso", rt, colorRetraso, Vector2.zero, Vector2.one, margen, Image.FillMethod.Horizontal);
        relleno = CrearBarra("Relleno", rt, colorFase1, Vector2.zero, Vector2.one, margen, Image.FillMethod.Horizontal);

        if (mostrarNombre) CrearNombre(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        raiz.SetActive(false);
    }

    void ConstruirVertical()
    {
        Rect sr = spriteMarco.rect;                          // píxeles en la textura (origen abajo-izquierda)
        float texAlto = spriteMarco.texture.height;
        float ancho = alturaMarco * sr.width / sr.height;

        raiz = new GameObject("BarraVidaJefe", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rt = (RectTransform)raiz.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = anclaje;
        rt.pivot = anclaje;
        rt.anchoredPosition = margenVertical;
        rt.sizeDelta = new Vector2(ancho, alturaMarco);

        grupo = raiz.GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Hueco del tubo, normalizado respecto al sprite (origen abajo-izquierda)
        float xMin = (interiorEnTextura.x - sr.x) / sr.width;
        float xMax = (interiorEnTextura.x + interiorEnTextura.width - sr.x) / sr.width;
        float yTop = texAlto - interiorEnTextura.y;
        float yBottom = texAlto - (interiorEnTextura.y + interiorEnTextura.height);
        float yMin = (yBottom - sr.y) / sr.height;
        float yMax = (yTop - sr.y) / sr.height;

        Vector2 aMin = new Vector2(xMin, yMin);
        Vector2 aMax = new Vector2(xMax, yMax);

        // Primero los rellenos (detrás) y el marco encima: el tubo del marco es transparente
        retraso = CrearBarra("Retraso", rt, colorRetraso, aMin, aMax, Vector2.zero, Image.FillMethod.Vertical);
        relleno = CrearBarra("Relleno", rt, colorFase1, aMin, aMax, Vector2.zero, Image.FillMethod.Vertical);

        GameObject goMarco = new GameObject("Marco", typeof(RectTransform));
        RectTransform rtM = (RectTransform)goMarco.transform;
        rtM.SetParent(rt, false);
        rtM.anchorMin = Vector2.zero;
        rtM.anchorMax = Vector2.one;
        rtM.offsetMin = Vector2.zero;
        rtM.offsetMax = Vector2.zero;
        Image marco = goMarco.AddComponent<Image>();
        marco.sprite = spriteMarco;
        marco.raycastTarget = false;

        if (mostrarNombre)
            CrearNombre(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(320f, 32f), new Vector2(0.5f, 0f));

        raiz.SetActive(false);
    }

    Image CrearBarra(string nombreObjeto, RectTransform padre, Color color, Vector2 anchorMin, Vector2 anchorMax,
                     Vector2 margen, Image.FillMethod metodo)
    {
        GameObject go = new GameObject(nombreObjeto, typeof(RectTransform));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(padre, false);
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = margen;
        r.offsetMax = -margen;

        Image img = go.AddComponent<Image>();
        img.sprite = spriteRelleno != null ? spriteRelleno : BossSprites.Cuadrado;
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = metodo;
        img.fillOrigin = 0;                       // izquierda (horizontal) o abajo (vertical)
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    void CrearNombre(RectTransform padre, Vector2 anchorMin, Vector2 anchorMax, Vector2 posicion, Vector2 tam, Vector2 pivote)
    {
        GameObject goT = new GameObject("Nombre", typeof(RectTransform));
        RectTransform rtT = (RectTransform)goT.transform;
        rtT.SetParent(padre, false);
        rtT.anchorMin = anchorMin;
        rtT.anchorMax = anchorMax;
        rtT.pivot = pivote;
        rtT.anchoredPosition = posicion;
        if (tam != Vector2.zero) rtT.sizeDelta = tam;
        else { rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero; }

        TextMeshProUGUI t = goT.AddComponent<TextMeshProUGUI>();
        t.text = nombre;
        t.fontSize = 18f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
    }

    // ---------- Mostrar / ocultar ----------

    void Mostrar()
    {
        if (raiz == null) return;
        pctRetraso = 1f;
        raiz.SetActive(true);
        StartCoroutine(Aparecer());
    }

    void AlMorir()
    {
        if (raiz != null) StartCoroutine(OcultarTras(1.5f));
    }

    IEnumerator Aparecer()
    {
        float t = 0f;
        while (t < duracionAparicion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, duracionAparicion));
            yield return null;
        }
        grupo.alpha = 1f;
    }

    IEnumerator OcultarTras(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (raiz != null) raiz.SetActive(false);
    }

    void Update()
    {
        if (raiz == null || !raiz.activeSelf || vida == null) return;

        float p = vida.Porcentaje;
        relleno.fillAmount = p;

        if (pctRetraso > p) pctRetraso = Mathf.MoveTowards(pctRetraso, p, velocidadRetraso * Time.deltaTime);
        else pctRetraso = p;
        retraso.fillAmount = pctRetraso;

        int fase = vida.Fase;
        relleno.color = fase >= 4 ? colorFase4 : (fase == 3 ? colorFase3 : (fase == 2 ? colorFase2 : colorFase1));
    }
}
