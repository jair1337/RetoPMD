using System.Collections.Generic;
using UnityEngine;

// Lista de objetivos a los que puede apuntar el autoapuntado del móvil (enemigos y jefe).
public static class ObjetivosJugador
{
    static readonly List<Transform> lista = new List<Transform>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        lista.Clear();
    }

    public static void Registrar(Transform t)
    {
        if (t != null && !lista.Contains(t)) lista.Add(t);
    }

    public static void Quitar(Transform t)
    {
        lista.Remove(t);
    }

    public static Transform MasCercano(Vector2 desde, float rango)
    {
        Transform mejor = null;
        float mejorSqr = rango * rango;

        for (int i = lista.Count - 1; i >= 0; i--)
        {
            Transform t = lista[i];
            if (t == null) { lista.RemoveAt(i); continue; }

            float d = ((Vector2)t.position - desde).sqrMagnitude;
            if (d < mejorSqr) { mejorSqr = d; mejor = t; }
        }
        return mejor;
    }
}
