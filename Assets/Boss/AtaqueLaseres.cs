using System.Collections;
using UnityEngine;

// ATAQUE 1: láseres que rodean al jefe con una separación angular fija y van rectos.
// Cada oleada avisa con una línea fina y luego dispara. Las oleadas alternas se desfasan
// media separación, así que hay que moverse hacia el hueco nuevo.
public class AtaqueLaseres : BossAttack
{
    [Header("Disposición")]
    public int cantidad = 12;
    [Tooltip("360 = rodea al jefe por completo. Menos = abanico apuntando al jugador.")]
    [Range(30f, 360f)] public float arco = 360f;
    public float anguloInicial = 90f;
    [Tooltip("Margen de separación entre el jefe y el inicio de cada láser.")]
    public float distanciaInicio = 0.8f;

    [Header("Oleadas")]
    public int oleadas = 3;
    public bool desfasarOleadas = true;
    public float pausaEntreOleadas = 0.4f;

    [Header("Tiempos")]
    public float tiempoAviso = 1f;
    public float duracionLaser = 1.2f;

    [Header("Aspecto")]
    public float anchoAviso = 0.05f;
    public float anchoLaser = 0.45f;
    [Tooltip("0 = automática según el tamaño de la arena.")]
    public float longitud = 0f;
    public Color colorAviso = new Color(1f, 0.25f, 0.25f, 1f);
    public Color colorLaser = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Rotación (0 = van rectos)")]
    public float velocidadRotacion = 0f;

    public override IEnumerator Ejecutar(BossController boss)
    {
        float largo = longitud > 0f ? longitud : boss.ArenaRadio * 2.5f;

        for (int o = 0; o < oleadas; o++)
        {
            float paso, inicio;
            if (arco >= 360f)
            {
                paso = 360f / cantidad;
                inicio = anguloInicial + (desfasarOleadas && o % 2 == 1 ? paso * 0.5f : 0f);
            }
            else
            {
                paso = cantidad > 1 ? arco / (cantidad - 1) : 0f;
                float centro = AnguloHaciaJugador(boss, boss.transform.position);
                inicio = centro - arco * 0.5f + (desfasarOleadas && o % 2 == 1 ? paso * 0.5f : 0f);
            }

            for (int i = 0; i < cantidad; i++)
            {
                BossLaser.Crear(boss, boss.transform, inicio + i * paso, distanciaInicio, largo,
                                tiempoAviso, duracionLaser, anchoAviso, anchoLaser, velocidadRotacion * boss.MultiplicadorVelocidad,
                                dano, colorAviso, colorLaser, estilo);
            }

            yield return new WaitForSeconds(tiempoAviso + duracionLaser);
            yield return Esperar(boss, pausaEntreOleadas);
        }
    }
}
