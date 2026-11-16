using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eco99.Editor
{
    [InitializeOnLoad]
    internal static class AbrirCementerio
    {
        private const string Ruta = "Assets/Scenes/Nivel3_Cementerio.unity";
        private const string Menu = "Assets/Scenes/Menu.unity";

        static AbrirCementerio()
        {
            EditorSceneManager.sceneOpened += AlAbrirEscena;
            EditorApplication.delayCall += AbrirMenuSiVacio;
        }

        private static void AbrirMenuSiVacio()
        {
            Scene escena = SceneManager.GetActiveScene();
            if (!EditorApplication.isPlayingOrWillChangePlaymode && escena.IsValid() &&
                string.IsNullOrEmpty(escena.path) && !escena.isDirty)
                EditorSceneManager.OpenScene(Menu);
        }

        private static void AlAbrirEscena(Scene escena, OpenSceneMode modo)
        {
            if (escena.path == Ruta && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += EnfocarMapa;
        }

        [MenuItem("Tools/Eco 99/Abrir Cementerio en 2D")]
        private static void Abrir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path == Ruta) EnfocarMapa();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Ruta);
        }

        private static void EnfocarMapa()
        {
            var vista = SceneView.lastActiveSceneView;
            if (vista == null || SceneManager.GetActiveScene().path != Ruta) return;
            vista.in2DMode = true;
            vista.Frame(new Bounds(new Vector3(13, 10, 0), new Vector3(26, 20, 1)), false);
        }
    }
}
