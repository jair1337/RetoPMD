using UnityEngine;

// Aspecto de las balas de un ataque del jefe: una hoja de sprites (rejilla uniforme) que se anima en bucle.
// Si 'hoja' está vacía, las balas usan el círculo de relleno (o BossController.spriteBala).
[System.Serializable]
public class EstiloBala
{
    [Tooltip("PNG con los frames en una rejilla uniforme (p. ej. proyectil_normal_jefe_32_32x32_12f).")]
    public Texture2D hoja;
    [Min(1)] public int columnas = 12;
    [Min(1)] public int filas = 1;
    [Tooltip("0 = todos los frames de la rejilla.")]
    public int frames = 0;
    public float fps = 14f;
    public float pixelsPorUnidad = 32f;
    [Tooltip("Parte del frame que ocupa el dibujo (el resto es margen transparente). Sirve para que 'Escala' sea el tamaño visible.")]
    [Range(0.2f, 1f)] public float fraccionVisible = 0.75f;
    public Color tinte = Color.white;
    [Tooltip("Multiplica SOLO el tamaño del dibujo, no la hitbox. Útil para balas alargadas (estelas, espadas).")]
    public float escalaSprite = 1f;

    public bool Valido { get { return hoja != null; } }

    // Copia de este estilo con otro color (para dibujar dos familias de balas con la misma hoja)
    public EstiloBala ConTinte(Color color)
    {
        EstiloBala e = (EstiloBala)MemberwiseClone();
        e.tinte = color;
        return e;
    }
}
