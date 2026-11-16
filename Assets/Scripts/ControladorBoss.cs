using UnityEngine;

namespace Eco99.Catacumbas
{
    public class ControladorBoss : MonoBehaviour
    {
        [Header("Vida")]
        public int vidaMaxima = 15;

        [Header("Sonidos")]
        public AudioClip sonidoMuerte;

        private int vidaActual;
        private bool derrotado;

        [Header("Movimiento")]
        public float velocidadMovimiento = 4f;

        [Header("Limites Arena")]
        public float limiteIzquierdo = 145f;
        public float limiteDerecho = 165f;
        public float limiteInferior = 30f;
        public float limiteSuperior = 45f;

        public float distanciaMinimaJugador = 4f;

        [Header("Disparo")]
        public GameObject prefabProyectil;
        public Transform puntoDeDisparo;
        public float tiempoEntreDisparos = 2f;
        public int numeroDeBalasEnAbanico = 5;
        public float separacionAngulo = 15f;

        private Transform jugador;
        private Vector2 destinoActual;
        private float siguienteDisparo;

        private Animator animator;

        void Start()
        {
            vidaActual = vidaMaxima;

            animator = GetComponent<Animator>();

            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");

            if (objJugador != null)
            {
                jugador = objJugador.transform;
            }

            ElegirNuevoDestino();
        }

        void Update()
        {
            if (derrotado) return;
            MovimientoBoss();
            GestionarDisparos();
        }

        void MovimientoBoss()
        {
            Vector2 posicionActual = transform.position;

            transform.position = Vector2.MoveTowards(
                posicionActual,
                destinoActual,
                velocidadMovimiento * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, destinoActual) < 0.5f)
            {
                ElegirNuevoDestino();
            }

            if (animator != null)
            {
                if (destinoActual.x > transform.position.x)
                {
                    animator.SetBool("movDr", true);
                    animator.SetBool("movIz", false);
                }
                else
                {
                    animator.SetBool("movIz", true);
                    animator.SetBool("movDr", false);
                }
            }
        }

        void ElegirNuevoDestino()
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 posibleDestino = new Vector2(
                    Random.Range(limiteIzquierdo, limiteDerecho),
                    Random.Range(limiteInferior, limiteSuperior)
                );

                if (jugador == null)
                {
                    destinoActual = posibleDestino;
                    return;
                }

                if (Vector2.Distance(posibleDestino, jugador.position) > distanciaMinimaJugador)
                {
                    destinoActual = posibleDestino;
                    return;
                }
            }
        }

        void GestionarDisparos()
        {
            if (jugador == null)
                return;

            if (Time.time >= siguienteDisparo)
            {
                DispararAbanico();
                siguienteDisparo = Time.time + tiempoEntreDisparos;
            }
        }

        void DispararAbanico()
        {
            Vector2 origen;

            if (puntoDeDisparo != null)
                origen = puntoDeDisparo.position;
            else
                origen = transform.position;

            Vector2 direccionBase =
                ((Vector2)jugador.position - origen).normalized;

            float anguloBase =
                Mathf.Atan2(direccionBase.y, direccionBase.x) *
                Mathf.Rad2Deg;

            float anguloInicial =
                anguloBase -
                ((numeroDeBalasEnAbanico - 1) * separacionAngulo * 0.5f);

            for (int i = 0; i < numeroDeBalasEnAbanico; i++)
            {
                float anguloActual =
                     anguloInicial + (i * separacionAngulo);

                Vector2 direccion = new Vector2(
                     Mathf.Cos(anguloActual * Mathf.Deg2Rad),
                     Mathf.Sin(anguloActual * Mathf.Deg2Rad)
                );

                GameObject bala = Instantiate(
                    prefabProyectil,
                    origen,
                    Quaternion.identity
                );

                DisparoCatacumbas scriptBala =
                    bala.GetComponent<DisparoCatacumbas>();

                if (scriptBala != null)
                {
                    scriptBala.ConfigurarProyectil(direccion, 10);
                }
            }
        }

        public void RecibirDanio(int cantidad)
        {
            if (derrotado) return;
            vidaActual -= cantidad;

            if (vidaActual <= 0)
            {
                Morir();
            }
        }

        [Header("Audio Victoria")]
        public AudioClip muerteboss; // Asegúrate de asignar tu clip aquí si lo prefieres arrastrar desde el inspector

        void Morir()
        {
            derrotado = true;
            Eco99.ProgresionEco99.CompletarNivel();

            // 1. Buscar el objeto de audio "Audioboss" y parar la música de fondo
            GameObject objetoMusica = GameObject.Find("Audioboss");
            if (objetoMusica != null)
            {
                AudioSource fuenteAudio = objetoMusica.GetComponent<AudioSource>();
                if (fuenteAudio != null)
                {
                    fuenteAudio.Stop(); // Para la música de las catacumbas

                    // 2. Reproducir el clip "muerteboss"
                    if (muerteboss != null)
                    {
                        fuenteAudio.clip = muerteboss;
                        fuenteAudio.loop = false; // Para que no se repita en bucle
                        fuenteAudio.Play();      // Suena el audio de muerte del boss
                    }
                }
            }

            // 3. Destruir al jefe un poco más tarde para que dé tiempo a escuchar el audio
            Destroy(gameObject, 2f);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.GetComponent<Bullet>() != null ||
                collision.CompareTag("Bullet") ||
                collision.name.Contains("Bullet"))
            {
                RecibirDanio(1);

                Destroy(collision.gameObject);
            }
        }
    }
}
