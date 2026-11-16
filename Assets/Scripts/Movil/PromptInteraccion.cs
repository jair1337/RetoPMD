using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cartel "Presiona E" en la parte baja de la pantalla. En móvil el propio cartel es un botón táctil.
// Se crea solo la primera vez que se usa, dentro del canvas de la UI.
public class PromptInteraccion : MonoBehaviour
{
    static PromptInteraccion instancia;

    GameObject panel;
    TMP_Text texto;
    object dueno;

    static PromptInteraccion Instancia
    {
        get
        {
            if (instancia == null)
            {
                Canvas canvas = CanvasUtil.Buscar();
                if (canvas == null) return null;
                GameObject go = new GameObject("PromptInteraccion", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                instancia = go.AddComponent<PromptInteraccion>();
                instancia.Construir();
            }
            return instancia;
        }
    }

    public static void Mostrar(object quien, string textoTeclado, string textoMovil)
    {
        PromptInteraccion p = Instancia;
        if (p == null) return;
        p.dueno = quien;
        p.texto.text = ControlesMoviles.Activos ? textoMovil : textoTeclado;
        p.panel.SetActive(true);
    }

    public static void Ocultar(object quien)
    {
        if (instancia == null || instancia.dueno != quien) return;
        instancia.dueno = null;
        instancia.panel.SetActive(false);
    }

    void Construir()
    {
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 150f);
        rt.sizeDelta = new Vector2(380f, 64f);

        panel = gameObject;

        Image fondo = gameObject.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.65f);
        fondo.raycastTarget = ControlesMoviles.Activos;

        if (ControlesMoviles.Activos)
        {
            Button b = gameObject.AddComponent<Button>();
            b.targetGraphic = fondo;
            b.onClick.AddListener(ControlesMoviles.SolicitarInteraccion);
        }

        GameObject goTexto = new GameObject("Texto", typeof(RectTransform));
        goTexto.transform.SetParent(transform, false);
        RectTransform rtT = (RectTransform)goTexto.transform;
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = new Vector2(10f, 4f);
        rtT.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI t = goTexto.AddComponent<TextMeshProUGUI>();
        t.fontSize = 30f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        texto = t;

        panel.SetActive(false);
    }
}
