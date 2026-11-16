using UnityEngine;
using UnityEngine.SceneManagement;

public class Controlador : MonoBehaviour
{
    // Carga un nivel específico por su nombre (Para el menú de selección de niveles)
    public void CargarNivel(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }

    // Avanza automáticamente al siguiente nivel en el Build Settings
    public void SiguienteNivel()
    {
        int escenaActual = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(escenaActual + 1);
    }

    // Reinicia el nivel actual (Para cuando el jugador muere)
    public void ReiniciarNivel()
    {
        int escenaActual = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(escenaActual);
    }

    // Vuelve al menú principal por su nombre
    public void IrAlMenu(string nombreEscenaMenu)
    {
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    // Cierra el juego (Para el botón Salir)
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
