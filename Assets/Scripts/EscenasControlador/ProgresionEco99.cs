using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eco99
{
    public sealed class ProgresionEco99 : MonoBehaviour
    {
        private string destino;
        private string mensaje;
        private GUIStyle estilo;

        public static void CompletarNivel()
        {
            Scene actual = SceneManager.GetActiveScene();
            string destino, titulo;
            switch (actual.name)
            {
                case "NivelPoblado":
                    destino = "Catacumbas";
                    titulo = "Poblado completado";
                    break;
                case "Catacumbas":
                    destino = "Nivel3_Cementerio";
                    titulo = "Catacumbas completadas";
                    break;
                default:
                    return;
            }

            if (FindFirstObjectByType<ProgresionEco99>() != null) return;
            var paso = new GameObject("Avance de nivel").AddComponent<ProgresionEco99>();
            paso.destino = destino;
            paso.mensaje = titulo;
            paso.StartCoroutine(paso.Cambiar());
        }

        private IEnumerator Cambiar()
        {
            yield return new WaitForSecondsRealtime(3f);
            SceneManager.LoadScene(destino);
        }

        private void OnGUI()
        {
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Clamp(Screen.height / 32, 14, 36)
                };
            }

            float ancho = Mathf.Min(Screen.width - 20, 520);
            var caja = new Rect((Screen.width - ancho) / 2, Screen.height * 0.39f, ancho, 84);
            GUI.Box(caja, "");
            GUI.Label(caja, mensaje, estilo);
        }
    }
}
