using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú: Herramientas > Animaciones > Autoconfigurar escena y prefabs
// Añade y configura AnimadorSprites en los enemigos de la escena, en el jefe y en los prefabs de balas
// de los enemigos, y asigna las hojas de sprites a cada ataque del jefe. Busca los PNG por nombre.
// Se puede ejecutar varias veces sin problema.
public static class AutoAnimaciones
{
    class Receta
    {
        public string hoja;
        public int columnas = 1;
        public int filas = 1;
        public int frames = 0;
        public float fps = 12f;
        public float impacto = 0.5f;
    }

    static Receta RecetaEnemigo(string nombre)
    {
        string n = nombre.ToLowerInvariant();
        if (n.Contains("esqueleto")) return new Receta { hoja = "Attack3 3", columnas = 6, fps = 12f, impacto = 0.5f };
        if (n.Contains("goblin")) return new Receta { hoja = "Attack3 2", columnas = 12, fps = 14f, impacto = 0.6f };
        if (n.Contains("champi")) return new Receta { hoja = "Attack3 1", columnas = 11, fps = 12f, impacto = 0.65f };
        if (n.Contains("perro")) return new Receta { hoja = "attack_3x7_128", columnas = 3, filas = 7, frames = 21, fps = 14f, impacto = 0.55f };
        if (n.Contains("pacman")) return new Receta { hoja = "GlacierShard_Attack_4f", columnas = 4, fps = 10f, impacto = 0.5f };
        return null; // p. ej. la esfera: sprite estático, solo efectos procedurales
    }

    static Receta RecetaProyectil(string nombre)
    {
        string n = nombre.ToLowerInvariant();
        if (n.Contains("helada")) return new Receta { hoja = "ice_shard_32_right_32x32_8f", columnas = 8, fps = 14f };
        if (n.Contains("electrica")) return new Receta { hoja = "lightning_ball_loop_32_32x32_12f", columnas = 12, fps = 14f };
        if (n.Contains("sagrada")) return new Receta { hoja = "holy_orb_loop_32_32x32_12f", columnas = 12, fps = 12f };
        return null;
    }

    static Texture2D Buscar(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Texture2D"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
        Debug.LogWarning("AutoAnimaciones: no encuentro la textura '" + nombre + "'");
        return null;
    }

    static void Rellenar(AnimadorSprites.Clip clip, Receta r)
    {
        Texture2D t = Buscar(r.hoja);
        if (t == null) return;
        clip.hoja = t;
        clip.columnas = r.columnas;
        clip.filas = r.filas;
        clip.frames = r.frames;
        clip.fps = r.fps;
    }

    static EstiloBala Estilo(string hoja, int columnas, float fps, float fraccionVisible)
    {
        EstiloBala e = new EstiloBala();
        e.hoja = Buscar(hoja);
        e.columnas = columnas;
        e.filas = 1;
        e.frames = 0;
        e.fps = fps;
        e.fraccionVisible = fraccionVisible;
        return e;
    }

    static AnimadorSprites Asegurar(GameObject go)
    {
        AnimadorSprites a = go.GetComponent<AnimadorSprites>();
        if (a == null) a = Undo.AddComponent<AnimadorSprites>(go);
        return a;
    }

    static void ConfigurarEnemigo(GameObject go)
    {
        AnimadorSprites a = Asegurar(go);
        Receta r = RecetaEnemigo(go.name);
        if (r != null)
        {
            Rellenar(a.atacar, r);
            a.momentoImpacto = r.impacto;
        }
        EditorUtility.SetDirty(a);
        PrefabUtility.RecordPrefabInstancePropertyModifications(a);
    }

    static void ConfigurarJefe(BossController boss)
    {
        AnimadorSprites a = Asegurar(boss.gameObject);

        // El jefe conserva el sprite que tenga en su SpriteRenderer (JefePoblado): sin hoja de Idle.
        a.idle = new AnimadorSprites.Clip();
        a.enabled = true;
        a.respirar = false;
        a.efectoAlMover = false;
        a.golpeDuracion = 0.5f;
        a.golpeEscala = 0.12f;
        a.duracionMuerte = 1.2f;

        boss.animador = a;
        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(boss);

        foreach (BossAttack ataque in boss.GetComponents<BossAttack>())
        {
            if (ataque is AtaqueLaseres) ataque.estilo = Estilo("laserjefe_128x32_128x32_8f", 8, 14f, 0.5f);
            else if (ataque is AtaqueCirculos) ataque.estilo = Estilo("proyectil_grande_jefe_32_32x32_12f", 12, 12f, 0.85f);
            else ataque.estilo = Estilo("proyectil_normal_jefe_32_32x32_12f", 12, 14f, 0.72f);

            EditorUtility.SetDirty(ataque);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ataque);
        }
    }

    static int ConfigurarPrefabsDeBalas()
    {
        int hechos = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Prefabs/Balas" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            Receta r = RecetaProyectil(Path.GetFileNameWithoutExtension(ruta));
            if (r == null) continue;

            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                if (raiz.GetComponent<SpriteRenderer>() == null) continue;

                AnimadorSprites a = raiz.GetComponent<AnimadorSprites>();
                if (a == null) a = raiz.AddComponent<AnimadorSprites>();

                Rellenar(a.idle, r);
                a.usarHijoVisual = false;
                a.respirar = false;
                a.efectoAlMover = false;

                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                hechos++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
        return hechos;
    }

    [MenuItem("Herramientas/Animaciones/Autoconfigurar escena y prefabs")]
    static void Todo()
    {
        int enemigos = 0, jefes = 0;

        foreach (EnemigoAI e in Object.FindObjectsByType<EnemigoAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ConfigurarEnemigo(e.gameObject);
            enemigos++;
        }

        foreach (EnemigoMeleeAI e in Object.FindObjectsByType<EnemigoMeleeAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ConfigurarEnemigo(e.gameObject);
            enemigos++;
        }

        foreach (BossController b in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ConfigurarJefe(b);
            jefes++;
        }

        int balas = ConfigurarPrefabsDeBalas();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        string resumen = "Enemigos: " + enemigos + " | Jefe: " + jefes + " | Prefabs de balas: " + balas
                         + "\nGuarda la escena (Ctrl+S) para conservar los cambios.";
        Debug.Log("AutoAnimaciones -> " + resumen);
        EditorUtility.DisplayDialog("Animaciones", resumen, "OK");
    }
}
