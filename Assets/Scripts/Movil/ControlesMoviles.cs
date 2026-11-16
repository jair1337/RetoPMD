using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controles táctiles. Se crea SOLO al cargar la escena (no hace falta añadirlo a mano).
// - Joystick flotante en la mitad izquierda de la pantalla (o los 4 botones de dirección si lo eliges).
// - Tu botón "Ataque" dispara mientras lo mantienes pulsado (con autoapuntado, lo gestiona JugadorDisparo).
// - Ajusta el CanvasScaler para que la UI se vea igual en cualquier móvil.
// - En PC los botones táctiles se ocultan. Para probar en el editor: Activacion = SiempreVisibles.
public class ControlesMoviles : MonoBehaviour
{
    public enum Activacion { Automatico, SiempreVisibles, NuncaVisibles }
    public enum ModoMovimiento { JoystickFlotante, BotonesDireccion }

    public static Vector2 Movimiento { get; private set; }
    public static bool Disparando { get; private set; }
    public static bool Activos { get; private set; }

    [Header("Cuándo mostrar los controles táctiles")]
    public Activacion activacion = Activacion.Automatico;
    public ModoMovimiento modoMovimiento = ModoMovimiento.JoystickFlotante;

    [Header("Canvas")]
    [Tooltip("Si se deja vacío se usa el canvas de la UI (UIJugador).")]
    public Canvas canvas;
    [Tooltip("Escala la UI según la pantalla (imprescindible en móvil).")]
    public bool ajustarCanvas = true;
    public Vector2 resolucionReferencia = new Vector2(1920f, 1080f);
    [Range(0f, 1f)] public float coincidirAnchoAlto = 0.5f;

    [Header("Botones existentes (se buscan por nombre en el canvas)")]
    public string nombreArriba = "Arriba";
    public string nombreAbajo = "Abajo";
    public string nombreIzquierda = "Izquierda";
    public string nombreDerecha = "Derecha";
    public string nombreAtaque = "Ataque";

    [Header("Botón de ataque (solo en táctil)")]
    [Tooltip("Lo mueve a la esquina inferior derecha, donde llega el pulgar.")]
    public bool reposicionarAtaque = true;
    public Vector2 margenAtaque = new Vector2(170f, 170f);
    public float escalaBotonAtaque = 1.5f;

    [Header("Joystick")]
    [Tooltip("Radio del joystick en unidades de canvas.")]
    public float radioJoystick = 110f;
    [Range(0.2f, 0.7f)] public float anchoZonaJoystick = 0.45f;

    bool arriba, abajo, izquierda, derecha;
    static bool interactuarPendiente;
    static float interactuarHasta;

    // ---------- Arranque automático ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Asegurar();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Asegurar();
    }

    static void Asegurar()
    {
        if (Object.FindFirstObjectByType<ControlesMoviles>() == null)
            new GameObject("ControlesMoviles").AddComponent<ControlesMoviles>();
    }

    // ---------- API estática ----------

    public static void SolicitarInteraccion()
    {
        interactuarPendiente = true;
        interactuarHasta = Time.unscaledTime + 0.4f;
    }

    public static bool ConsumirInteractuar()
    {
        if (!interactuarPendiente) return false;
        interactuarPendiente = false;
        return Time.unscaledTime <= interactuarHasta;
    }

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        Movimiento = Vector2.zero;
        Disparando = false;
        interactuarPendiente = false;

        switch (activacion)
        {
            case Activacion.SiempreVisibles: Activos = true; break;
            case Activacion.NuncaVisibles: Activos = false; break;
            default: Activos = Application.isMobilePlatform || Touchscreen.current != null; break;
        }

        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = 60;           // Android va a 30 FPS por defecto
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }

    void Start()
    {
        if (canvas == null) canvas = CanvasUtil.Buscar();
        if (canvas == null) return;

        if (ajustarCanvas) AjustarEscalado();

        Button bArriba = BuscarBoton(nombreArriba);
        Button bAbajo = BuscarBoton(nombreAbajo);
        Button bIzq = BuscarBoton(nombreIzquierda);
        Button bDer = BuscarBoton(nombreDerecha);
        Button bAtaque = BuscarBoton(nombreAtaque);

        if (!Activos)
        {
            // En PC los botones táctiles no sirven: se ocultan
            Ocultar(bArriba); Ocultar(bAbajo); Ocultar(bIzq); Ocultar(bDer); Ocultar(bAtaque);
            return;
        }

        bool usarJoystick = modoMovimiento == ModoMovimiento.JoystickFlotante;
        if (usarJoystick)
        {
            Ocultar(bArriba); Ocultar(bAbajo); Ocultar(bIzq); Ocultar(bDer);
            CrearJoystick();
        }
        else
        {
            Mantener(bArriba, v => { arriba = v; Recalcular(); });
            Mantener(bAbajo, v => { abajo = v; Recalcular(); });
            Mantener(bIzq, v => { izquierda = v; Recalcular(); });
            Mantener(bDer, v => { derecha = v; Recalcular(); });
        }

        Mantener(bAtaque, v => Disparando = v);

        if (bAtaque != null)
        {
            RectTransform rt = (RectTransform)bAtaque.transform;
            if (reposicionarAtaque)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-margenAtaque.x, margenAtaque.y);
            }
            rt.localScale = Vector3.one * escalaBotonAtaque;
        }
    }

    void OnDestroy()
    {
        Movimiento = Vector2.zero;
        Disparando = false;
    }

    // ---------- Utilidades ----------

    void AjustarEscalado()
    {
        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) return;
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = resolucionReferencia;
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = coincidirAnchoAlto;
    }

    Button BuscarBoton(string nombre)
    {
        foreach (Button b in canvas.GetComponentsInChildren<Button>(true))
            if (b.name == nombre) return b;
        return null;
    }

    static void Ocultar(Button b)
    {
        if (b != null) b.gameObject.SetActive(false);
    }

    static void Mantener(Button b, System.Action<bool> alCambiar)
    {
        if (b == null) return;
        BotonMantener m = b.GetComponent<BotonMantener>();
        if (m == null) m = b.gameObject.AddComponent<BotonMantener>();
        m.alCambiar = alCambiar;
    }

    void Recalcular()
    {
        Vector2 v = new Vector2((derecha ? 1f : 0f) - (izquierda ? 1f : 0f), (arriba ? 1f : 0f) - (abajo ? 1f : 0f));
        Movimiento = v.normalized;
    }

    void CrearJoystick()
    {
        GameObject go = new GameObject("ZonaJoystick", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();    // detrás del resto de la UI

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(anchoZonaJoystick, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.004f);
        img.raycastTarget = true;
        img.canvasRenderer.cullTransparentMesh = false;

        RectTransform baseVis = CrearCirculoUI("Base", rt, radioJoystick * 2f, new Color(1f, 1f, 1f, 0.18f));
        RectTransform knob = CrearCirculoUI("Knob", rt, radioJoystick * 0.7f, new Color(1f, 1f, 1f, 0.45f));
        baseVis.gameObject.SetActive(false);
        knob.gameObject.SetActive(false);

        ZonaJoystick z = go.AddComponent<ZonaJoystick>();
        z.zona = rt;
        z.baseVis = baseVis;
        z.knob = knob;
        z.radio = radioJoystick;
        z.alMover = v => Movimiento = v;
    }

    static RectTransform CrearCirculoUI(string nombre, RectTransform padre, float tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(tamano, tamano);

        Image i = go.AddComponent<Image>();
        i.sprite = BossSprites.Circulo;
        i.color = color;
        i.raycastTarget = false;
        return r;
    }
}

// Busca el canvas de la UI del juego (prefiere el llamado UIJugador).
public static class CanvasUtil
{
    public static Canvas Buscar()
    {
        Canvas mejor = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace) continue;
            if (c.name.Contains("UIJugador")) return c;
            if (mejor == null) mejor = c;
        }
        return mejor;
    }
}

// Un botón de UI que avisa mientras se mantiene pulsado.
public class BotonMantener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public System.Action<bool> alCambiar;
    bool pulsado;

    public void OnPointerDown(PointerEventData e) { Poner(true); }
    public void OnPointerUp(PointerEventData e) { Poner(false); }
    public void OnPointerExit(PointerEventData e) { Poner(false); }
    void OnDisable() { Poner(false); }

    void Poner(bool v)
    {
        if (pulsado == v) return;
        pulsado = v;
        if (alCambiar != null) alCambiar(v);
    }
}

// Joystick flotante: aparece donde pones el dedo.
public class ZonaJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform zona, baseVis, knob;
    public float radio = 110f;
    public System.Action<Vector2> alMover;

    const int SinPuntero = -1000;
    int punteroId = SinPuntero;
    Vector2 centro;

    public void OnPointerDown(PointerEventData e)
    {
        if (punteroId != SinPuntero) return;
        punteroId = e.pointerId;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(zona, e.position, e.pressEventCamera, out centro);
        baseVis.anchoredPosition = centro;
        knob.anchoredPosition = centro;
        baseVis.gameObject.SetActive(true);
        knob.gameObject.SetActive(true);
        Enviar(Vector2.zero);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != punteroId) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(zona, e.position, e.pressEventCamera, out local);

        Vector2 v = Vector2.ClampMagnitude((local - centro) / radio, 1f);
        knob.anchoredPosition = centro + v * radio;
        Enviar(v.magnitude < 0.15f ? Vector2.zero : v);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != punteroId) return;
        punteroId = SinPuntero;
        baseVis.gameObject.SetActive(false);
        knob.gameObject.SetActive(false);
        Enviar(Vector2.zero);
    }

    void OnDisable()
    {
        punteroId = SinPuntero;
        Enviar(Vector2.zero);
    }

    void Enviar(Vector2 v)
    {
        if (alMover != null) alMover(v);
    }
}
