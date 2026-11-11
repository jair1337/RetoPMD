using UnityEngine;
using UnityEngine.InputSystem;

public class JugadorDisparo : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint; // El punto de salida de la bala (cañón)
    
    [Header("Mirilla (Cursor)")]
    public Texture2D crosshairTexture;
    public Vector2 hotSpot = new Vector2(16, 16);

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Activa la mirilla personalizada en el cursor del ratón
        if (crosshairTexture != null)
        {
            Cursor.SetCursor(crosshairTexture, hotSpot, CursorMode.Auto);
        }
    }

    // Se ejecuta al hacer clic izquierdo gracias al Input System
    void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            ShootTowardsMouse();
        }
    }

    void ShootTowardsMouse()
    {
        if (bulletPrefab == null) return;

        // 1. Obtener la posición del ratón en el mundo del juego
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        // 2. Calcular la dirección desde el punto de disparo hasta el ratón
        Vector2 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)mouseWorldPos - spawnPos).normalized;

        // 3. Calcular el ángulo exacto (sin restar 90 grados para que coincida con transform.right)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // 4. Instanciar la bala
        Instantiate(bulletPrefab, spawnPos, rotation);
    }
}