using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú: Herramientas > Jefe > Configurar jefe, escudo y barra (escena abierta)
// Trabaja SOLO sobre la escena que tengas abierta (abre la del Poblado). Se puede repetir sin problema.
//  - Deja el sprite del jefe como está (JefePoblado) y activa su animador sin el Idle viejo.
//  - Añade los ataques nuevos (Rejilla, Orbes, Dash) con sus sprites de balas.
//  - Pone el script EscudoJefe al objeto "Escudo".
//  - Deja la barra de vida del jefe horizontal y sin sprite.
public static class ConfigurarJefeNivel
{
    static Texture2D Buscar(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Texture2D"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
        Debug.LogWarning("ConfigurarJefe: no encuentro la textura '" + nombre + "'");
        return null;
    }

    static Sprite BuscarSprite(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Texture2D"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) != nombre) continue;

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                Sprite s = o as Sprite;
                if (s != null) return s;
            }
        }
        Debug.LogWarning("ConfigurarJefe: no encuentro el sprite '" + nombre + "'");
        return null;
    }

    static EstiloBala Estilo(string hoja, int columnas, int frames, float fps, float ppu, float fraccionVisible, float escalaSprite)
    {
        EstiloBala e = new EstiloBala();
        e.hoja = Buscar(hoja);
        e.columnas = columnas;
        e.filas = 1;
        e.frames = frames;
        e.fps = fps;
        e.pixelsPorUnidad = ppu;
        e.fraccionVisible = fraccionVisible;
        e.escalaSprite = escalaSprite;
        return e;
    }

    static EstiloBala EstiloNormal() { return Estilo("proyectil_normal_jefe_32_32x32_12f", 12, 0, 14f, 32f, 0.72f, 1f); }
    static EstiloBala EstiloGrande() { return Estilo("proyectil_grande_jefe_32_32x32_12f", 12, 0, 12f, 32f, 0.85f, 1f); }

    static T Asegurar<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    [MenuItem("Herramientas/Jefe/Configurar jefe, escudo y barra (escena abierta)")]
    static void Configurar()
    {
        BossController boss = Object.FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
        if (boss == null)
        {
            EditorUtility.DisplayDialog("Jefe", "No hay ningún jefe (BossController) en la escena abierta.\nAbre la escena del Poblado y vuelve a probar.", "OK");
            return;
        }

        GameObject go = boss.gameObject;
        string informe = "";

        // 1) Animador del jefe: conserva su sprite (JefePoblado) y no usa el Idle viejo
        AnimadorSprites anim = Asegurar<AnimadorSprites>(go);
        anim.idle = new AnimadorSprites.Clip();
        anim.enabled = true;
        anim.respirar = false;
        anim.efectoAlMover = false;
        anim.golpeDuracion = 0.5f;
        anim.golpeEscala = 0.12f;
        anim.duracionMuerte = 1.2f;
        boss.animador = anim;
        EditorUtility.SetDirty(anim);
        informe += "Animador del jefe: activo, sin el Idle viejo\n";

        // 2) Ataques nuevos
        AtaqueRejilla rejilla = Asegurar<AtaqueRejilla>(go);
        rejilla.estilo = Estilo("JefeBala", 1, 1, 1f, 100f, 1f, 4f);     // estela blanca alargada (se tiñe por familia)
        EditorUtility.SetDirty(rejilla);

        AtaqueOrbes orbes = Asegurar<AtaqueOrbes>(go);
        orbes.estilo = EstiloNormal();
        orbes.estiloOrbe = EstiloGrande();
        EditorUtility.SetDirty(orbes);

        AtaqueDash dash = Asegurar<AtaqueDash>(go);
        dash.estilo = EstiloNormal();
        EditorUtility.SetDirty(dash);
        informe += "Ataques nuevos: Rejilla (fase 3), Orbes y Dash (fase 4)\n";

        // 3) Escudo
        GameObject escudo = GameObject.Find("Escudo");
        if (escudo != null)
        {
            Asegurar<EscudoJefe>(escudo);

            SpriteRenderer sEscudo = escudo.GetComponent<SpriteRenderer>();
            SpriteRenderer sJefe = go.GetComponent<SpriteRenderer>();
            if (sEscudo != null && sJefe != null && sEscudo.sortingOrder <= sJefe.sortingOrder)
            {
                sEscudo.sortingOrder = sJefe.sortingOrder + 1;           // el escudo se dibuja delante del jefe
                EditorUtility.SetDirty(sEscudo);
            }
            informe += "Escudo: sigue al jefe, recibe balas y se pone rojo\n";
        }
        else
        {
            informe += "AVISO: no encuentro ningún objeto llamado 'Escudo' en la escena\n";
        }

        // 4) Barra de vida con el sprite del marco
        BarraVidaJefeUI barra = Asegurar<BarraVidaJefeUI>(go);
        barra.usarMarco = false;                      // barra horizontal sencilla (sin sprite de marco)
        barra.spriteMarco = null;
        EditorUtility.SetDirty(barra);
        informe += "Barra de vida: horizontal sencilla (sin sprite)\n";

        EditorUtility.SetDirty(boss);
        EditorSceneManager.MarkSceneDirty(go.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("ConfigurarJefe ->\n" + informe);
        EditorUtility.DisplayDialog("Jefe", informe + "\nGuarda la escena (Ctrl+S).", "OK");
    }
}
