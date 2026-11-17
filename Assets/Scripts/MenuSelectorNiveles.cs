using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSelectorNiveles : MonoBehaviour
{
    [Header("Panel de Selección de Niveles")]
    public GameObject panelSeleccionNiveles; // Aquí arrastraremos el panel

    // 1. Asigna esto al evento On Click() del botón "Jugar" principal
    public void AbrirSeleccionNiveles()
    {
        if (panelSeleccionNiveles != null)
        {
            panelSeleccionNiveles.SetActive(true); // Muestra los 3 niveles
        }
    }

    // 2. Asigna esto a un botón de "Volver" o "Cerrar" dentro del panel
    public void CerrarSeleccionNiveles()
    {
        if (panelSeleccionNiveles != null)
        {
            panelSeleccionNiveles.SetActive(false); // Oculta los niveles
        }
    }

    // 3. Asigna esto a cada uno de los 3 botones de nivel (escribe el nombre exacto de la escena)
    public void CargarNivel(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }

    // 4. Salir del juego
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}