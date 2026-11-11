using UnityEngine;
using UnityEngine.UI; // Importante para UI antigua
// using UnityEngine.UIElements; // Usar si estás en Unity muy nuevo y usas Toolkit

public class Vida : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    // REFERENCIA A LA UI
    public Slider VidaSlider; 

    void Start()
    {
        currentHealth = maxHealth;
        
        // Configurar Slider al inicio
        if (VidaSlider != null)
        {
            VidaSlider.maxValue = maxHealth;
            VidaSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // ACTUALIZAR UI
        if (VidaSlider != null)
        {
            VidaSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Jugador Muerto");
            // Lógica de muerte aquí
        }
    }
}
