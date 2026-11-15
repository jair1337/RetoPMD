using UnityEngine;

public class Cofre : MonoBehaviour
{
    [Header("Configuración del Cofre")]
    public bool cofreAbierto = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !cofreAbierto)
        {
            cofreAbierto = true;
            Debug.Log("¡Cofre abierto! (Premio pendiente de definir)");

            // Aquí pondremos la lógica del nuevo premio más adelante
        }
    }
}