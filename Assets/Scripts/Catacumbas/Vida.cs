using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Eco99.Catacumbas
{
    // Importante para UI antigua
    // using UnityEngine.UIElements; // Usar si estás en Unity muy nuevo y usas Toolkit

    public class Vida : MonoBehaviour
    {
        public int maxHealth = 100;
        private int currentHealth;
        private bool muerto;

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
            if (muerto) return;
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
                muerto = true;
                if (TryGetComponent(out PlayerMovement2D movimiento)) movimiento.enabled = false;
                if (TryGetComponent(out JugadorDisparo disparo)) disparo.enabled = false;
                if (TryGetComponent(out Rigidbody2D cuerpo)) cuerpo.linearVelocity = Vector2.zero;
                Invoke(nameof(Reiniciar), 1.5f);
            }
        }
        private void Reiniciar() => SceneManager.LoadScene(gameObject.scene.path);
    }

}
