using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida del jefe dentro de tu canvas (UIJugador). Se construye sola, empieza oculta y aparece
// cuando EncuentroJefe activa al jefe. EncuentroJefe la añade automáticamente si no existe.
// Los sprites son opcionales: sin ellos usa rectángulos de color.
public class BarraVidaJefeUI : MonoBehaviour
{
    [Header("Referencias (se buscan solas)")]
    public BossVida vida;
    public Canvas canvas;

    [Header("Tamaño y posición")]
    public Vector2 tamano = new Vector2(560f, 26f);
    public float margenSuperior = 36f;

    [Header("Nombre")]
    public bool mostrarNombre = true;
    public string nombre = "JEFE";

    [Header("Aspecto (sprites opcionales)")]
    public Sprite spriteMarco;
    public Sprite spriteRelleno;
    public Color colorMarco = new Color(0.05f, 0.03f, 0.08f, 0.85f);
    public Color colorFase1 = new Color(0.8f, 0.15f, 0.2f, 1f);
    public Color colorFase2 = new Color(0.95f, 0.5f, 0.1f, 1f);
    public Color colorFase3 = new Color(0.65f, 0.2f, 0.9f, 1f);
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

        Construir();
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

    void Construir()
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
        marco.sprite = spriteMarco;
        marco.color = spriteMarco != null ? Color.white : colorMarco;
        marco.raycastTarget = false;

        retraso = CrearBarra("Retraso", rt, colorRetraso, 4f);
        relleno = CrearBarra("Relleno", rt, colorFase1, 4f);

        if (mostrarNombre)
        {
            GameObject goT = new GameObject("Nombre", typeof(RectTransform));
            RectTransform rtT = (RectTransform)goT.transform;
            rtT.SetParent(rt, false);
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;
            rtT.offsetMin = Vector2.zero;
            rtT.offsetMax = Vector2.zero;

            TextMeshProUGUI t = goT.AddComponent<TextMeshProUGUI>();
            t.text = nombre;
            t.fontSize = 18f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
        }

        raiz.SetActive(false);
    }

    Image CrearBarra(string nombreObjeto, RectTransform padre, Color color, float margen)
    {
        GameObject go = new GameObject(nombreObjeto, typeof(RectTransform));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(padre, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(margen, margen);
        r.offsetMax = new Vector2(-margen, -margen);

        Image img = go.AddComponent<Image>();
        img.sprite = spriteRelleno != null ? spriteRelleno : BossSprites.Cuadrado;
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

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

        relleno.color = vida.Fase >= 3 ? colorFase3 : (vida.Fase == 2 ? colorFase2 : colorFase1);
    }
}
