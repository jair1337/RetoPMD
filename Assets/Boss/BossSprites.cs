using UnityEngine;

// Sprites generados por código para no depender de texturas (placeholders).
// Cuando tengas arte real, asigna tus sprites o cambia aquí el sprite del círculo.
public static class BossSprites
{
    static Sprite circulo, cuadrado, cuadradoIzquierda;
    static Transform raiz;

    // Objeto padre de todos los avisos y láseres (para poder limpiarlos de golpe)
    public static Transform Raiz
    {
        get
        {
            if (raiz == null) raiz = new GameObject("BossFX").transform;
            return raiz;
        }
    }

    public static void LimpiarEfectos()
    {
        if (raiz == null) return;
        for (int i = raiz.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(raiz.GetChild(i).gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        circulo = null; cuadrado = null; cuadradoIzquierda = null; raiz = null;
    }

    // Círculo de 1 unidad de diámetro
    public static Sprite Circulo
    {
        get
        {
            if (circulo == null)
            {
                const int tam = 64;
                var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                float c = (tam - 1) * 0.5f;
                for (int y = 0; y < tam; y++)
                {
                    for (int x = 0; x < tam; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (tam * 0.5f);
                        float a = Mathf.Clamp01((1f - d) / 0.12f); // borde suave
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                circulo = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam);
            }
            return circulo;
        }
    }

    // Cuadrado de 1x1 con el pivote en el centro
    public static Sprite Cuadrado
    {
        get
        {
            if (cuadrado == null) cuadrado = CrearCuadrado(new Vector2(0.5f, 0.5f));
            return cuadrado;
        }
    }

    // Cuadrado de 1x1 con el pivote en el centro del lado izquierdo (ideal para láseres)
    public static Sprite CuadradoIzquierda
    {
        get
        {
            if (cuadradoIzquierda == null) cuadradoIzquierda = CrearCuadrado(new Vector2(0f, 0.5f));
            return cuadradoIzquierda;
        }
    }

    static Sprite CrearCuadrado(Vector2 pivote)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixeles = new Color[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        tex.SetPixels(pixeles);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), pivote, 4);
    }

    // Crea un sprite suelto (marcadores, avisos...)
    public static SpriteRenderer Crear(string nombre, Sprite sprite, Vector2 pos, Vector2 escala, Color color, int orden)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(Raiz, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = orden;
        return sr;
    }
}
