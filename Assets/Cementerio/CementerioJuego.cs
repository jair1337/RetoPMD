using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Eco99
{
    public sealed class CementerioJuego : MonoBehaviour
    {
        [SerializeField] private Transform mapa;
        [SerializeField] private Material materialSprites;
        [SerializeField] private Transform[] manzanas;
        [SerializeField] private Transform encuentros;

        private const float RadioJugador = 0.33f;
        private const int VidaMaxima = 12;
        private const float Ancho = 26f;
        private const float Alto = 20f;
        private static readonly Color Marfil = new Color32(249, 230, 194, 255);
        private static readonly Color Oro = new Color32(245, 170, 95, 255);
        private static readonly Color Verde = new Color32(97, 193, 154, 255);
        private readonly List<Enemigo> enemigos = new List<Enemigo>();
        private readonly List<Proyectil> disparos = new List<Proyectil>();
        private readonly List<Rect> barreras = new List<Rect>();
        private readonly bool[] explorado = new bool[9];

        private enum Estado { Portada, Partida, Victoria, Derrota }
        private Estado estado;
        private Sprite[] arteJugadorIdle, arteJugadorCorre, arteZombi, arteZombiAtaque, arteJefe;
        private Sprite pulso, orbe;
        private SpriteRenderer jugador, sombra;
        private Camera camara;
        private AudioSource sonido;
        private Enemigo guardian;
        private AudioClip tiro, golpe, cura, final;
        private Vector2 posicion, direccion = Vector2.right;
        private float reloj, ultimoDano = -10f, siguienteDisparo;
        private int vida = VidaMaxima, bajas, zonas;
        private GUIStyle titulo, texto, detalle, boton;

        private sealed class Enemigo
        {
            public Vector2 inicio, posicion;
            public SpriteRenderer imagen;
            public float siguiente, aviso;
            public int vida, fase;
            public bool activado, jefe;
        }

        private sealed class Proyectil
        {
            public Vector2 posicion, velocidad;
            public SpriteRenderer imagen;
            public float restante;
            public bool aliado;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            posicion = new Vector2(13, 2.5f);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            arteJugadorIdle = CargarAnimacion("jugadorIdle", 4);
            arteJugadorCorre = CargarAnimacion("jugadorCorre", 8);
            arteZombi = CargarAnimacion("zombi", 8);
            arteZombiAtaque = CargarAnimacion("zombiAtaque", 4);
            arteJefe = CargarAnimacion("jefe", 4);
            pulso = Resources.Load<Sprite>("pulso");
            orbe = Resources.Load<Sprite>("orbe");
            jugador = Dibujar("Jugador", arteJugadorIdle[0], posicion, 1.25f, 30);
            sombra = Dibujar("Niebla", Resources.Load<Sprite>("niebla"), posicion, 1, 100);
            #if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.instance.Hide(sombra.gameObject, false);
            #endif
            camara = new GameObject("Camara del Cementerio").AddComponent<Camera>();
            camara.transform.SetParent(transform);
            camara.orthographic = true;
            camara.orthographicSize = 6.3f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color32(12, 20, 26, 255);
            camara.gameObject.AddComponent<AudioListener>();
            sonido = gameObject.AddComponent<AudioSource>();
            tiro = Tono("Disparo", 610, 180, 0.1f);
            golpe = Tono("Impacto", 160, 65, 0.16f);
            cura = Tono("Manzana", 320, 760, 0.24f);
            final = Tono("Final", 180, 510, 0.38f);

            Physics2D.SyncTransforms();
            foreach (var collider in mapa.GetComponentsInChildren<BoxCollider2D>())
            {
                if (collider.transform.name.StartsWith("verjas")) continue;
                Bounds b = collider.bounds;
                barreras.Add(new Rect(b.min.x, b.min.y, b.size.x, b.size.y));
            }
            foreach (Transform encuentro in encuentros)
            {
                Vector2 centro = encuentro.position;
                if (encuentro.name == "Jefe") CrearEnemigo(centro, true);
                else
                {
                    CrearEnemigo(centro + Vector2.left * 1.35f, false);
                    CrearEnemigo(centro + Vector2.right * 1.35f, false);
                }
            }
            ActualizarCamara();
        }

        private static Sprite[] CargarAnimacion(string nombre, int fotogramas)
        {
            var sprites = new Sprite[fotogramas];
            for (int i = 0; i < sprites.Length; i++)
                sprites[i] = Resources.Load<Sprite>(nombre + i);
            return sprites;
        }

        private SpriteRenderer Dibujar(string nombre, Sprite arte, Vector2 punto, float escala, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform);
            objeto.transform.position = punto;
            objeto.transform.localScale = Vector3.one * escala;
            var imagen = objeto.AddComponent<SpriteRenderer>();
            imagen.sharedMaterial = materialSprites;
            imagen.sprite = arte;
            imagen.sortingOrder = orden;
            return imagen;
        }

        private void CrearEnemigo(Vector2 inicio, bool jefe)
        {
            var imagen = Dibujar(jefe ? "Guardian del Cementerio" : "Zombi " + (enemigos.Count + 1),
                jefe ? arteJefe[0] : arteZombi[0], inicio, jefe ? 1.62f : 1.23f, 29);
            imagen.enabled = false;
            var enemigo = new Enemigo { inicio = inicio, posicion = inicio, imagen = imagen,
                jefe = jefe, vida = jefe ? 18 : 3, fase = enemigos.Count };
            enemigos.Add(enemigo);
            if (jefe) guardian = enemigo;
        }

        private void Update()
        {
            if (estado == Estado.Portada)
            {
                if (Keyboard.current?.enterKey.wasPressedThisFrame == true || Pulsado(BotonFinal(24, 174)))
                    estado = Estado.Partida;
                return;
            }
            if (estado != Estado.Partida)
            {
                if (Keyboard.current?.rKey.wasPressedThisFrame == true || Pulsado(BotonFinal(24, 174)))
                    Reiniciar();
                else if (estado == Estado.Victoria &&
                    (Keyboard.current?.enterKey.wasPressedThisFrame == true || Pulsado(BotonFinal(209, 190))))
                    SceneManager.LoadScene("Menu");
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.04f);
            reloj += dt;
            MoverJugador(dt);
            ActualizarCamara();
            DispararJugador();
            MoverEnemigos(dt);
            MoverProyectiles(dt);
            if (vida <= 0)
            {
                estado = Estado.Derrota;
                sonido.PlayOneShot(golpe, 0.3f);
                return;
            }
            RecogerManzanas();
            Explorar();
            if (bajas == enemigos.Count && zonas == explorado.Length)
            {
                estado = Estado.Victoria;
                sonido.PlayOneShot(final, 0.26f);
            }
        }

        private void MoverJugador(float dt)
        {
            Vector2 marcha = Vector2.zero;
            if (Keyboard.current is Keyboard k)
            {
                float x = Eje(k.dKey.isPressed || k.rightArrowKey.isPressed, k.aKey.isPressed || k.leftArrowKey.isPressed);
                float y = Eje(k.wKey.isPressed || k.upArrowKey.isPressed, k.sKey.isPressed || k.downArrowKey.isPressed);
                marcha = Vector2.ClampMagnitude(new Vector2(x, y), 1);
            }
            if (marcha == Vector2.zero) marcha = ControlesMoviles.Movimiento;
            if (marcha == Vector2.zero && Touchscreen.current is Touchscreen tactil)
            {
                foreach (var toque in tactil.touches)
                {
                    if (!toque.press.isPressed || toque.startPosition.ReadValue().x >= Screen.width / 2f) continue;
                    marcha = Vector2.ClampMagnitude((toque.position.ReadValue() - toque.startPosition.ReadValue()) / 65f, 1);
                    break;
                }
            }
            if (marcha.sqrMagnitude > 0.05f && ControlesMoviles.Activos) direccion = marcha.normalized;
            posicion = Avanzar(posicion, marcha * (5.1f * dt), RadioJugador);
            jugador.transform.position = posicion;
            var animacion = marcha == Vector2.zero ? arteJugadorIdle : arteJugadorCorre;
            jugador.sprite = animacion[(int)(reloj * 12f) % animacion.Length];
            jugador.color = reloj - ultimoDano < 0.6f && (int)(reloj * 17) % 2 == 0
                ? (Color)new Color32(255, 110, 115, 255) : Color.white;
        }

        private static int Eje(bool positivo, bool negativo) => (positivo ? 1 : 0) - (negativo ? 1 : 0);

        private void ActualizarCamara()
        {
            float mitadY = camara.orthographicSize;
            float mitadX = mitadY * camara.aspect;
            float x = mitadX < Ancho / 2 ? Mathf.Clamp(posicion.x, mitadX, Ancho - mitadX) : Ancho / 2;
            float y = Mathf.Clamp(posicion.y, mitadY, Alto - mitadY);
            camara.transform.position = new Vector3(x, y, -10);
            sombra.transform.position = posicion;
        }

        private void DispararJugador()
        {
            bool disparar = ControlesMoviles.Disparando;
            if (!disparar && Touchscreen.current is Touchscreen tactil)
            {
                foreach (var toque in tactil.touches)
                {
                    if (toque.press.isPressed && toque.startPosition.ReadValue().x >= Screen.width / 2f)
                    {
                        disparar = true;
                        break;
                    }
                }
            }
            if (disparar)
            {
                Enemigo cercano = null;
                float distancia = 7f;
                foreach (Enemigo enemigo in enemigos)
                {
                    float actual = Vector2.Distance(posicion, enemigo.posicion);
                    if (!enemigo.activado || enemigo.vida <= 0 || actual >= distancia ||
                        !LineaLibre(posicion, enemigo.posicion)) continue;
                    cercano = enemigo;
                    distancia = actual;
                }
                if (cercano != null) direccion = (cercano.posicion - posicion).normalized;
            }
            else if (Mouse.current is Mouse mouse)
            {
                Vector2 punto = mouse.position.ReadValue();
                Vector2 apuntado = (Vector2)camara.ScreenToWorldPoint(new Vector3(punto.x, punto.y, 10)) - posicion;
                if (apuntado.sqrMagnitude > 0.01f) direccion = apuntado.normalized;
                disparar = mouse.leftButton.isPressed;
            }
            jugador.flipX = direccion.x < 0;
            if (!disparar || reloj < siguienteDisparo) return;
            siguienteDisparo = reloj + 0.19f;
            Disparar(posicion, direccion, true, 13f);
            sonido.PlayOneShot(tiro, 0.11f);
        }

        private void MoverEnemigos(float dt)
        {
            foreach (Enemigo e in enemigos)
            {
                if (e.vida <= 0) continue;

                Vector2 hacia = posicion - e.posicion;
                float distancia = hacia.magnitude;
                Vector2 centro = e.jefe ? e.inicio : e.inicio + Vector2.right * (e.fase % 2 == 0 ? 1.35f : -1.35f);
                if (!e.activado && Vector2.Distance(posicion, centro) < 6.2f)
                {
                    e.activado = true;
                    e.siguiente = reloj + 0.55f + e.fase % 2 * 0.25f;
                    e.imagen.enabled = true;
                }
                if (!e.activado) continue;

                bool ve = distancia < 6.3f && LineaLibre(e.posicion, posicion);
                float angulo = reloj * 0.9f + e.fase * Mathf.PI;
                Vector2 patrulla = e.inicio + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 0.95f;
                Vector2 rumbo = (patrulla - e.posicion).normalized;
                if (ve) rumbo = distancia > (e.jefe ? 3.8f : 2.8f) ? hacia.normalized : Vector2.zero;
                if (ve && distancia < 1.05f) rumbo = -hacia.normalized;

                if (e.aviso > 0)
                {
                    if (reloj >= e.aviso)
                    {
                        if (ve) Rafaga(e, hacia.normalized);
                        e.aviso = 0;
                        e.siguiente = reloj + (e.jefe ? 1.24f : 1.44f + e.fase % 2 * 0.22f);
                    }
                    rumbo = Vector2.zero;
                }
                else if (ve && reloj >= e.siguiente)
                {
                    e.aviso = reloj + 0.28f;
                }

                e.posicion = Avanzar(e.posicion, rumbo * (dt * (e.jefe ? 1.45f : 1.95f)), e.jefe ? 0.49f : 0.34f);
                e.imagen.transform.position = e.posicion;
                e.imagen.flipX = hacia.x < 0;
                if (!e.jefe && e.aviso > 0)
                {
                    float progreso = Mathf.Clamp01(1f - (e.aviso - reloj) / 0.28f);
                    e.imagen.sprite = arteZombiAtaque[Mathf.Min(arteZombiAtaque.Length - 1,
                        (int)(progreso * arteZombiAtaque.Length))];
                }
                else
                {
                    var animacion = e.jefe ? arteJefe : arteZombi;
                    e.imagen.sprite = animacion[rumbo == Vector2.zero ? 0 : (int)(reloj * 10f) % animacion.Length];
                }
                e.imagen.color = e.aviso > 0 ? new Color32(255, 147, 168, 255) : Color.white;
                if (distancia < (e.jefe ? 0.79f : 0.63f)) DanoJugador();
            }
        }

        private void Rafaga(Enemigo enemigo, Vector2 rumbo)
        {
            int apertura = enemigo.jefe ? (enemigo.vida <= 9 ? 3 : 2) : 0;
            for (int i = -apertura; i <= apertura; i++)
            {
                Vector2 salida = Quaternion.Euler(0, 0, i * 18) * rumbo;
                Disparar(enemigo.posicion, salida, false, enemigo.jefe ? 4.8f : 4.2f);
            }
        }

        private void Disparar(Vector2 origen, Vector2 rumbo, bool aliado, float velocidad)
        {
            var imagen = Dibujar(aliado ? "Pulso" : "Orbe enemigo", aliado ? pulso : orbe,
                origen, aliado ? 0.85f : 1.55f, 45);
            disparos.Add(new Proyectil { posicion = origen, velocidad = rumbo * velocidad,
                imagen = imagen, restante = 2.7f, aliado = aliado });
        }

        private void MoverProyectiles(float dt)
        {
            for (int i = disparos.Count - 1; i >= 0; i--)
            {
                Proyectil p = disparos[i];
                p.restante -= dt;
                Vector2 paso = p.velocidad * dt;
                int trozos = Mathf.Max(1, Mathf.CeilToInt(paso.magnitude / 0.12f));
                bool impacto = false;
                for (int s = 0; s < trozos && !impacto; s++)
                {
                    p.posicion += paso / trozos;
                    impacto = Bloqueado(p.posicion, 0.06f) || ImpactoActor(p);
                }
                if (impacto || p.restante <= 0)
                {
                    Destroy(p.imagen.gameObject);
                    disparos.RemoveAt(i);
                }
                else
                {
                    p.imagen.transform.position = p.posicion;
                    if (!p.aliado) p.imagen.transform.Rotate(0, 0, 210f * dt);
                }
            }
        }

        private bool ImpactoActor(Proyectil p)
        {
            if (!p.aliado)
            {
                if (Vector2.Distance(p.posicion, posicion) >= 0.45f) return false;
                DanoJugador();
                return true;
            }
            foreach (Enemigo e in enemigos)
            {
                if (!e.activado || e.vida <= 0 || Vector2.Distance(e.posicion, p.posicion) > (e.jefe ? 0.65f : 0.45f))
                    continue;

                e.vida--;
                if (e.vida == 0)
                {
                    bajas++;
                    e.imagen.enabled = false;
                    sonido.PlayOneShot(golpe, 0.16f);
                }
                return true;
            }
            return false;
        }

        private void DanoJugador()
        {
            if (reloj - ultimoDano < 0.65f || vida <= 0) return;
            ultimoDano = reloj;
            vida--;
            sonido.PlayOneShot(golpe, 0.22f);
        }

        private void RecogerManzanas()
        {
            if (vida == VidaMaxima) return;
            foreach (Transform manzana in manzanas)
            {
                if (!manzana.gameObject.activeSelf || Vector2.Distance(manzana.position, posicion) > 0.58f) continue;
                vida = Mathf.Min(VidaMaxima, vida + 3);
                manzana.gameObject.SetActive(false);
                sonido.PlayOneShot(cura, 0.25f);
                if (vida == VidaMaxima) return;
            }
        }

        private void Explorar()
        {
            int x = Mathf.Clamp((int)(posicion.x * 3 / Ancho), 0, 2);
            int y = Mathf.Clamp((int)(posicion.y * 3 / Alto), 0, 2);
            int indice = y * 3 + x;
            if (explorado[indice]) return;
            explorado[indice] = true;
            zonas++;
        }

        private Vector2 Avanzar(Vector2 origen, Vector2 delta, float radio)
        {
            Vector2 pasoX = new Vector2(origen.x + delta.x, origen.y);
            if (!Bloqueado(pasoX, radio)) origen = pasoX;
            Vector2 pasoY = new Vector2(origen.x, origen.y + delta.y);
            return Bloqueado(pasoY, radio) ? origen : pasoY;
        }

        private bool Bloqueado(Vector2 punto, float radio)
        {
            if (punto.x < 0.8f + radio || punto.x > Ancho - 0.8f - radio ||
                punto.y < 0.8f + radio || punto.y > Alto - 0.8f - radio) return true;
            foreach (Rect r in barreras)
            {
                float x = punto.x - Mathf.Clamp(punto.x, r.xMin, r.xMax);
                float y = punto.y - Mathf.Clamp(punto.y, r.yMin, r.yMax);
                if (x * x + y * y <= radio * radio) return true;
            }
            return false;
        }

        private bool LineaLibre(Vector2 inicio, Vector2 fin)
        {
            int pasos = Mathf.CeilToInt(Vector2.Distance(inicio, fin) / 0.22f);
            for (int i = 1; i < pasos; i++)
                if (Bloqueado(Vector2.Lerp(inicio, fin, (float)i / pasos), 0.06f)) return false;
            return true;
        }

        private static AudioClip Tono(string nombre, float inicio, float cierre, float segundos)
        {
            const int frecuencia = 22050;
            var datos = new float[Mathf.CeilToInt(frecuencia * segundos)];
            float fase = 0;
            for (int i = 0; i < datos.Length; i++)
            {
                float t = (float)i / datos.Length;
                fase += Mathf.Lerp(inicio, cierre, t) * (2 * Mathf.PI / frecuencia);
                datos[i] = Mathf.Sin(fase) * Mathf.Min(t * 26f, 1f) * (1 - t) * 0.72f;
            }
            AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private void Reiniciar() => SceneManager.LoadScene(gameObject.scene.path);

        private void OnDestroy()
        {
            if (tiro != null) Destroy(tiro);
            if (golpe != null) Destroy(golpe);
            if (cura != null) Destroy(cura);
            if (final != null) Destroy(final);
        }

        private void OnGUI()
        {
            if (titulo == null) CrearEstilos();
            float escala = EscalaGui;
            Matrix4x4 anterior = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1));
            float w = Screen.width / escala, h = Screen.height / escala;
            if (estado != Estado.Portada) DibujarHUD(w);
            if (estado != Estado.Partida) DibujarPantalla(w, h);
            GUI.matrix = anterior;
        }

        private static float EscalaGui => Mathf.Max(0.1f, Mathf.Min(Screen.width / 1000f, Screen.height / 620f));

        private static Rect BotonFinal(float margen, float ancho)
        {
            return new Rect((Screen.width / EscalaGui - 572) / 2 + margen,
                (Screen.height / EscalaGui - 246) / 2 + 186, ancho, 38);
        }

        private static bool Pulsado(Rect boton)
        {
            Vector2 pantalla;
            if (Mouse.current is Mouse mouse && mouse.leftButton.wasPressedThisFrame)
                pantalla = mouse.position.ReadValue();
            else if (Touchscreen.current is Touchscreen tactil && tactil.primaryTouch.press.wasPressedThisFrame)
                pantalla = tactil.primaryTouch.position.ReadValue();
            else return false;

            return boton.Contains(new Vector2(pantalla.x, Screen.height - pantalla.y) / EscalaGui);
        }

        private void CrearEstilos()
        {
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            texto = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            detalle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            boton = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = Marfil;
            texto.normal.textColor = Marfil;
            detalle.normal.textColor = new Color32(170, 197, 180, 255);
        }

        private static void Rellenar(Rect rect, Color color)
        {
            Color anterior = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = anterior;
        }

        private void DibujarHUD(float w)
        {
            Rellenar(new Rect(22, 22, 312, 92), new Color32(15, 26, 32, 236));
            GUI.Label(new Rect(34, 27, 270, 25), "VIDA   " + vida + " / " + VidaMaxima, texto);
            Rellenar(new Rect(36, 58, 280, 9), new Color32(54, 65, 65, 255));
            Rellenar(new Rect(36, 58, 280f * vida / VidaMaxima, 9), vida <= 3 ? Oro : Verde);
            GUI.Label(new Rect(34, 75, 280, 24), "Enemigos " + (enemigos.Count - bajas) + "    Sectores " + zonas + " / 9", detalle);

            float x = w - 118;
            Rellenar(new Rect(x - 28, 22, 124, 132), new Color32(15, 26, 32, 236));
            GUI.Label(new Rect(x - 17, 29, 100, 24), "MAPA", detalle);
            for (int i = 0; i < explorado.Length; i++)
                Rellenar(new Rect(x + i % 3 * 22, 54 + (2 - i / 3) * 22, 19, 19),
                    explorado[i] ? Verde : (Color)new Color32(53, 71, 69, 255));
            Rellenar(new Rect(x + posicion.x / Ancho * 64 - 2, 54 + (1 - posicion.y / Alto) * 64 - 2, 5, 5), Marfil);

            if (guardian == null || !guardian.activado || guardian.vida <= 0) return;
            GUI.Label(new Rect(w / 2 - 104, 24, 260, 25), "Guardián del Cementerio", detalle);
            Rellenar(new Rect(w / 2 - 104, 51, 208, 7), new Color32(63, 43, 50, 255));
            Rellenar(new Rect(w / 2 - 104, 51, 208f * guardian.vida / 18f, 7), new Color32(232, 99, 135, 255));
        }

        private void DibujarPantalla(float w, float h)
        {
            float x = (w - 572) / 2, y = (h - 246) / 2;
            Rellenar(new Rect(0, 0, w, h), new Color32(8, 16, 25, 135));
            Rellenar(new Rect(x, y, 572, 246), new Color32(20, 31, 39, 255));
            Rellenar(new Rect(x, y, 4, 246), Oro);
            bool portada = estado == Estado.Portada;
            string encabezado = portada ? "Cementerio" : estado == Estado.Victoria ? "Cementerio purgado" : "Has caído";
            GUI.Label(new Rect(x + 24, y + 18, 510, 24), "ECO 99   /   NIVEL 3", detalle);
            GUI.Label(new Rect(x + 22, y + 42, 530, 59), encabezado, titulo);
            GUI.Label(new Rect(x + 24, y + 112, 530, 33),
                portada ? "Explora los 9 sectores y derrota al guardián y sus zombis."
                    : "Sectores explorados " + zonas + " / 9   ·   Enemigos vencidos " + bajas + " / " + enemigos.Count, detalle);
            if (portada)
            {
                GUI.Label(new Rect(x + 24, y + 145, 530, 32), Application.isMobilePlatform
                    ? "Arrastra a la izquierda para moverte · Toca a la derecha para disparar"
                    : "WASD o flechas para moverte   ·   Clic izquierdo para disparar", detalle);
                GUI.Box(BotonFinal(24, 174), "Jugar  ↵", boton);
            }
            else
            {
                GUI.Box(BotonFinal(24, 174), "Reiniciar  R", boton);
                if (estado == Estado.Victoria)
                    GUI.Box(BotonFinal(209, 190), "Volver al menú", boton);
            }
        }
    }
}
