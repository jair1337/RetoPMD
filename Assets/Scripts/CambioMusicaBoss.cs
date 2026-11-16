using UnityEngine;

namespace Eco99.Catacumbas
{
    public class CambioMusicaBoss : MonoBehaviour
    {
        public AudioSource musicaNormal;
        public AudioSource musicaBoss;

        private bool activado = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (activado) return;

            if (collision.CompareTag("Player"))
            {
                activado = true;

                musicaNormal.Stop();
                musicaBoss.Play();
            }
        }
    }
}
