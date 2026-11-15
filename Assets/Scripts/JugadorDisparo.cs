using UnityEngine;
using UnityEngine.InputSystem;

public class JugadorDisparo : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint; // El punto de salida de la bala (cañón)
    [Tooltip("Segundos entre disparos.")]
    public float cadencia = 0.18f;
    [Tooltip("Si está activo, mantener el clic dispara en ráfaga.")]
    public bool disparoAutomatico = true;
    [Tooltip("Desvío aleatorio máximo en grados (0 = puntería exacta).")]
    [Range(0f, 15f)] public float dispersion = 0f;

    [Header("Móvil (autoapuntado)")]
    [Tooltip("En móvil apunta al enemigo más cercano dentro de este radio. Si no hay, dispara hacia donde caminas.")]
    public float rangoAutoApuntado = 14f;

    [Header("Mirilla (Cursor)")]
    public Texture2D crosshairTexture;
    public Vector2 hotSpot = new Vector2(16, 16);

    private Camera mainCamera;
    private Vida vida;
    private bool gatilloMouse;
    private float proximoDisparo;
    private Vector2 ultimaDireccion = Vector2.right;

    void Start()
    {
        mainCamera = Camera.main;
        vida = GetComponent<Vida>();

        if (crosshairTexture != null)
        {
            Cursor.SetCursor(crosshairTexture, hotSpot, CursorMode.Auto);
        }
    }

    // Se ejecuta al pulsar y al soltar el clic izquierdo (Input System)
    void OnFire(InputValue value)
    {
        gatilloMouse = value.isPressed;

        if (value.isPressed && !disparoAutomatico) IntentarDisparar(true);
    }

    void Update()
    {
        if (ControlesMoviles.Movimiento.sqrMagnitude > 0.04f)
            ultimaDireccion = ControlesMoviles.Movimiento.normalized;

        if (gatilloMouse && disparoAutomatico) IntentarDisparar(true);
        else if (!gatilloMouse && ControlesMoviles.Disparando) IntentarDisparar(false);
    }

    void IntentarDisparar(bool conRaton)
    {
        if (vida != null && vida.EstaMuerto) return;
        if (Time.time < proximoDisparo) return;

        Vector2 origen = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 direccion;
        if (!CalcularDireccion(origen, conRaton, out direccion)) return;

        proximoDisparo = Time.time + cadencia;
        Disparar(origen, direccion);
    }

    bool CalcularDireccion(Vector2 origen, bool conRaton, out Vector2 direccion)
    {
        direccion = ultimaDireccion;

        if (conRaton)
        {
            if (Mouse.current == null) return false;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return false;

            Vector2 pantalla = Mouse.current.position.ReadValue();
            Vector3 mundo = mainCamera.ScreenToWorldPoint(pantalla);
            direccion = ((Vector2)mundo - origen).normalized;
            return true;
        }

        // Táctil: autoapuntado al enemigo (o jefe) más cercano
        Transform objetivo = ObjetivosJugador.MasCercano(origen, rangoAutoApuntado);
        if (objetivo != null) direccion = ((Vector2)objetivo.position - origen).normalized;
        return true;
    }

    void Disparar(Vector2 origen, Vector2 direccion)
    {
        if (bulletPrefab == null) return;

        float angle = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        if (dispersion > 0f) angle += Random.Range(-dispersion, dispersion);

        Instantiate(bulletPrefab, origen, Quaternion.Euler(0, 0, angle));
    }
}
