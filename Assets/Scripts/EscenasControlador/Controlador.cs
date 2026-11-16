using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class Controlador : MonoBehaviour
{
    // Llama a esta función desde el botón Jugar
    public void Jugar()
    {
        // Carga la siguiente escena en la lista de Build Settings (índice 1)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        
        // Opcional: también puedes cargarla por su nombre exacto así:
        // SceneManager.LoadScene("01_Nivel_Parte1");
    }

    // Llama a esta función desde el botón Salir
    public void Salir()
    {
        Debug.Log("Saliendo del juego..."); // Para comprobar que funciona en el editor
        Application.Quit();
    }
}
