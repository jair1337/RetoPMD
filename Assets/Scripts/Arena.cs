using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class Arena : MonoBehaviour
{
    [Range(16, 128)]
    public int resolucion = 64; // Suavidad del círculo
    public float radio = 0.5f;  // 0.5 encaja exacto con el Sprite Circle por defecto de Unity

    void Start()
    {
        GenerarCirculo();
    }

    // Esto permite verlo y ajustarlo en el editor sin darle a Play
    void OnValidate()
    {
        GenerarCirculo();
    }

    void GenerarCirculo()
    {
        EdgeCollider2D edge = GetComponent<EdgeCollider2D>();
        Vector2[] puntos = new Vector2[resolucion + 1];

        for (int i = 0; i <= resolucion; i++)
        {
            float angulo = i * Mathf.PI * 2f / resolucion;
            puntos[i] = new Vector2(Mathf.Cos(angulo) * radio, Mathf.Sin(angulo) * radio);
        }

        edge.points = puntos;
    }
}