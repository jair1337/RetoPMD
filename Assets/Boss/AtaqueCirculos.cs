using System.Collections;
using UnityEngine;

// ATAQUE 3: red de espirales. Desde el centro del jefe salen "brazos" de balas grandes que se curvan:
// unos hacia un lado y otros hacia el otro. Al cruzarse forman una malla por la que hay que colarse.
public class AtaqueCirculos : BossAttack
{
    [Header("Duración")]
    public float duracion = 6f;
    [Tooltip("Segundos entre cada tanda de balas. Menos = cadena más densa (separación = velocidad x intervalo).")]
    public float intervalo = 0.3f;

    [Header("Brazos")]
    public int brazosAntihorarios = 5;
    public int brazosHorarios = 5;
    [Tooltip("Brazos que van en línea recta (0 = ninguno).")]
    public int brazosRectos = 0;
    [Tooltip("Grados por segundo que gira la trayectoria de cada bala. Más = curva más cerrada.")]
    public float curvatura = 28f;
    [Tooltip("Grados que gira el punto de salida en cada tanda (teje el dibujo).")]
    public float giroEmisor = 3f;

    [Header("Balas")]
    public float velocidad = 4f;
    public float escala = 0.8f;
    public Color color = new Color(0.6f, 0.95f, 1f, 1f);
    [Tooltip("Seguridad: las balas también desaparecen al salir de la arena.")]
    public float vidaBala = 14f;

    public override IEnumerator Ejecutar(BossController boss)
    {
        float anguloBase = Random.Range(0f, 360f);
        float t = 0f;

        while (t < duracion)
        {
            Vector2 origen = boss.transform.position;

            Emitir(boss, origen, anguloBase, brazosAntihorarios, curvatura, 0f);
            Emitir(boss, origen, anguloBase, brazosHorarios, -curvatura, 0.5f);
            Emitir(boss, origen, anguloBase, brazosRectos, 0f, 0.25f);

            anguloBase += giroEmisor;
            float espera = Intervalo(boss, intervalo);
            t += espera;
            yield return new WaitForSeconds(espera);
        }
    }

    // desfase: fracción de paso entre brazos, para que los grupos queden intercalados
    void Emitir(BossController boss, Vector2 origen, float anguloBase, int brazos, float giro, float desfase)
    {
        if (brazos <= 0) return;

        float paso = 360f / brazos;
        for (int b = 0; b < brazos; b++)
        {
            float ang = anguloBase + (b + desfase) * paso;
            boss.Disparar(origen, ang, velocidad, escala, color, vidaBala, dano, 0f, giro, estilo);
        }
    }
}
