using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Ponlo en el objeto de la puerta (LimiteCentral). Cuando TODOS los interruptores están activos,
// desactiva los colliders que bloquean el paso. Muestra un contador en el canvas: activados y restantes.
public class PuertaInterruptores : MonoBehaviour
{
    [Tooltip("Si se deja vacío usa todos los interruptores de la escena.")]
    public Interruptor[] interruptores;
    [Tooltip("Si se deja vacío usa los colliders de este objeto.")]
    public Collider2D[] bloqueos;
    [Tooltip("Objetos que se ocultan al abrir (reja, sprite de puerta...). Opcional.")]
    public GameObject[] ocultarAlAbrir;

    [Header("Contador en pantalla")]
    [Tooltip("Si se deja vacío, se crea un texto automáticamente en el canvas de la UI.")]
    public TMP_Text textoContador;
    [Tooltip("{0} = activados, {1} = total, {2} = que faltan.")]
    public string textoFormato = "Faroles: {0}/{1}  (faltan {2})";
    public string textoAbierta = "¡Puerta abierta!";
    [Tooltip("Segundos que se muestra el mensaje final antes de ocultar el contador.")]
    public float segundosMensajeFinal = 4f;
    [Tooltip("Posición del texto automático respecto a la esquina superior izquierda.")]
    public Vector2 posicionContador = new Vector2(30f, -30f);
    public float tamanoLetra = 32f;
    public Color colorTexto = new Color(1f, 0.92f, 0.5f, 1f);

    [Header("Eventos")]
    public UnityEvent alAbrirse;

    public bool Abierta { get; private set; }
    public int Activados { get; private set; }
    public int Total { get { return interruptores != null ? interruptores.Length : 0; } }
    public int Faltan { get { return Total - Activados; } }

    void Start()
    {
        if (interruptores == null || interruptores.Length == 0)
            interruptores = FindObjectsByType<Interruptor>(FindObjectsSortMode.None);

        if (bloqueos == null || bloqueos.Length == 0)
            bloqueos = GetComponents<Collider2D>();

        foreach (Interruptor i in interruptores)
            if (i != null) i.AlEncenderse += AlEncenderseInterruptor;

        if (textoContador == null) CrearTexto();
        ActualizarTexto();
    }

    void OnDestroy()
    {
        if (interruptores == null) return;
        foreach (Interruptor i in interruptores)
            if (i != null) i.AlEncenderse -= AlEncenderseInterruptor;
    }

    void CrearTexto()
    {
        Canvas canvas = CanvasUtil.Buscar();
        if (canvas == null) return;

        GameObject go = new GameObject("ContadorInterruptores", typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicionContador;
        rt.sizeDelta = new Vector2(620f, 50f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = tamanoLetra;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Left;
        t.color = colorTexto;
        t.raycastTarget = false;
        textoContador = t;
    }

    void AlEncenderseInterruptor(Interruptor _)
    {
        Activados = 0;
        foreach (Interruptor i in interruptores)
            if (i != null && i.Activado) Activados++;

        ActualizarTexto();

        if (!Abierta && Activados >= Total) Abrir();
    }

    void ActualizarTexto()
    {
        if (textoContador != null)
            textoContador.text = string.Format(textoFormato, Activados, Total, Faltan);
    }

    public void Abrir()
    {
        if (Abierta) return;
        Abierta = true;

        foreach (Collider2D c in bloqueos)
            if (c != null) c.enabled = false;

        foreach (GameObject go in ocultarAlAbrir)
            if (go != null) go.SetActive(false);

        if (textoContador != null)
        {
            textoContador.text = textoAbierta;
            StartCoroutine(OcultarContador());
        }

        Debug.Log("Puerta abierta");
        alAbrirse?.Invoke();
    }

    IEnumerator OcultarContador()
    {
        yield return new WaitForSeconds(segundosMensajeFinal);
        if (textoContador != null) textoContador.enabled = false;
    }
}