using System.Collections;
using UnityEngine;

// ATAQUE 2: bullet hell. Anillos de balas que se expanden (con hueco) y/o espiral continua.
public class AtaqueBalas : BossAttack
{
    public enum Modo { Anillos, Espiral, Ambos }
    public Modo modo = Modo.Ambos;

    [Header("Anillos")]
    public int anillos = 8;
    public int balasPorAnillo = 36;
    public float intervaloAnillos = 0.45f;
    public float velocidadAnillo = 4.5f;
    public float escalaAnillo = 0.3f;
    [Tooltip("Ancho en grados del hueco por el que se puede pasar. 0 = sin hueco.")]
    public float huecoAncho = 35f;
    [Tooltip("El hueco cae cerca de la dirección del jugador (± esta variación en grados).")]
    public float variacionHueco = 40f;
    public bool desfasarAnillos = true;

    [Header("Espiral")]
    public float duracionEspiral = 4f;
    public int brazos = 3;
    public float intervaloEspiral = 0.06f;
    [Tooltip("Grados que gira la espiral en cada disparo.")]
    public float giroPorDisparo = 11f;
    public float velocidadEspiral = 5f;
    public float escalaEspiral = 0.28f;

    [Header("Aspecto")]
    public Color color = new Color(0.25f, 0.9f, 1f, 1f);
    public float vidaBala = 8f;

    public override IEnumerator Ejecutar(BossController boss)
    {
        if (modo == Modo.Anillos || modo == Modo.Ambos)
            yield return StartCoroutine(RutinaAnillos(boss));

        if (modo == Modo.Espiral || modo == Modo.Ambos)
            yield return StartCoroutine(RutinaEspiral(boss));
    }

    IEnumerator RutinaAnillos(BossController boss)
    {
        float paso = 360f / balasPorAnillo;

        for (int i = 0; i < anillos; i++)
        {
            Vector2 origen = boss.transform.position;
            float baseAng = desfasarAnillos && i % 2 == 1 ? paso * 0.5f : 0f;
            float centroHueco = AnguloHaciaJugador(boss, origen) + Random.Range(-variacionHueco, variacionHueco);

            boss.Anillo(origen, balasPorAnillo, baseAng, 0f, velocidadAnillo, escalaAnillo, color,
                        vidaBala, dano, centroHueco, huecoAncho, 0f, 0f, estilo);

            yield return Esperar(boss, intervaloAnillos);
        }
    }

    IEnumerator RutinaEspiral(BossController boss)
    {
        float t = 0f;
        float angulo = Random.Range(0f, 360f);

        while (t < duracionEspiral)
        {
            Vector2 origen = boss.transform.position;
            for (int b = 0; b < brazos; b++)
            {
                float ang = angulo + b * 360f / brazos;
                boss.Disparar(origen, ang, velocidadEspiral, escalaEspiral, color, vidaBala, dano, 0f, 0f, estilo);
            }

            angulo += giroPorDisparo;
            float espera = Intervalo(boss, intervaloEspiral);
            t += espera;
            yield return new WaitForSeconds(espera);
        }
    }
}
