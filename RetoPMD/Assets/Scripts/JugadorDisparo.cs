using UnityEngine;
using UnityEngine.InputSystem;

public class JugadorDisparo : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint; // El punto de salida de la bala

    [Header("Mirilla (Cursor)")]
    public Texture2D crosshairTexture;
    public Vector2 hotSpot = new Vector2(16, 16);

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (crosshairTexture != null)
        {
            Cursor.SetCursor(crosshairTexture, hotSpot, CursorMode.Auto);
        }
    }

    void Update()
    {
        // Ya NO rotamos el transform del jugador aquí para evitar que gire como una peonza.
        // Si quieres que solo rote el firePoint, puedes rotar "firePoint" en lugar de "transform".
        ActualizarRotacionFirePoint();
    }

    void ActualizarRotacionFirePoint()
    {
        if (firePoint == null || mainCamera == null) return;
        if (Mouse.current == null) return;

        // Convertimos la posición de la pantalla del ratón al mundo usando la posición de la cámara libremente
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;

        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Rota únicamente el firePoint (el cañón)
        firePoint.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // Instancia la bala en la posición y rotación del firePoint
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }
}