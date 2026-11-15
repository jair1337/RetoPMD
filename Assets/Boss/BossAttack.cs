using System.Collections;
using UnityEngine;

// Clase base de todos los ataques. Para crear un ataque nuevo:
//   public class MiAtaque : BossAttack { public override IEnumerator Ejecutar(BossController boss) { ... } }
// y añádelo como componente al objeto del jefe.
public abstract class BossAttack : MonoBehaviour
{
    [Header("General")]
    public int dano = 20;

    [Header("Sprites de las balas de este ataque")]
    public EstiloBala estilo = new EstiloBala();

    public abstract IEnumerator Ejecutar(BossController boss);

    // Segundos reales de espera: se acortan según la fase del jefe (más frecuencia)
    // y se alargan un poco en los combos para no saturar la pantalla.
    protected float Intervalo(BossController boss, float segundos)
    {
        float s = segundos / Mathf.Max(0.1f, boss.MultiplicadorFrecuencia);
        return boss.EnCombo ? s * boss.relajarCombo : s;
    }

    protected WaitForSeconds Esperar(BossController boss, float segundos)
    {
        return new WaitForSeconds(Intervalo(boss, segundos));
    }

    // Ángulo (en grados) desde 'desde' hasta el jugador. Si no hay jugador, uno aleatorio.
    protected float AnguloHaciaJugador(BossController boss, Vector2 desde)
    {
        if (boss.jugador == null) return Random.Range(0f, 360f);
        Vector2 d = (Vector2)boss.jugador.position - desde;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    // Hace parpadear un sprite de aviso durante 'tiempo' segundos y luego lo destruye.
    protected IEnumerator Parpadeo(SpriteRenderer sr, float tiempo, Color color, float alphaMin, float alphaMax, float frecuencia = 4f)
    {
        float t = 0f;
        while (t < tiempo)
        {
            if (sr == null) yield break;
            t += Time.deltaTime;
            float progreso = Mathf.Clamp01(t / tiempo);
            float k = 0.5f + 0.5f * Mathf.Sin(t * frecuencia * Mathf.PI * 2f);
            color.a = Mathf.Lerp(alphaMin, alphaMax, k) * Mathf.Lerp(0.5f, 1f, progreso);
            sr.color = color;
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}
