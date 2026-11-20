// ═══════════════════════════════════════════════════════════════════════════
//  SensorVision.cs — LA PIEZA PRINCIPAL DE LA PRÁCTICA
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  ⚠️  LO QUE OS DOY AQUÍ ESTÁ DELIBERADAMENTE MAL.
//
//  Tal y como está, este sensor detecta por DISTANCIA Y NADA MÁS. Es decir,
//  es exactamente el primero de los errores típicos de la sesión 4: el guardia
//  os ve a través de las paredes y de espaldas.
//
//  Está así a propósito. Arrancad el proyecto, jugad treinta segundos y veréis
//  el problema con vuestros propios ojos. Vuestro trabajo en el apartado A del
//  enunciado es convertir esto en un sensor que se pueda defender:
//
//     ETAPA 1  alcance   ✅ hecho (es lo único que hay)
//     ETAPA 2  ángulo    ❌ TODO
//     ETAPA 3  oclusión  ❌ TODO   ← y con VARIOS puntos, no solo el centro
//     MEDIDOR  sospecha  ❌ TODO   ← 3 factores, decaimiento, histéresis
//
//  Filtrar barato antes de filtrar caro (sesión 4): el orden de las tres
//  etapas no es casual. La comprobación de distancia es un producto escalar;
//  el raycast es una consulta al motor de físicas. Poner el raycast primero
//  «funciona» y cuesta veinte veces más.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class SensorVision : MonoBehaviour, ISensor
    {
        [Header("Objetivo")]
        [Tooltip("El jugador. Si lo dejáis vacío se busca por tag al arrancar.")]
        public Transform Objetivo;
        public string TagObjetivo = "Player";

        [Header("Cono de visión")]
        public float Alcance = 15f;
        [Range(0f, 180f)] public float AnguloVision = 60f;

        [Tooltip("Desde dónde se mira. Si está vacío se usa el propio transform. " +
                 "Poned aquí la cabeza: mirar desde los pies da resultados raros.")]
        public Transform Ojos;

        [Tooltip("Qué capas TAPAN la visión. Marcad aquí las paredes. " +
                 "Si dejáis esto en Nothing, no habrá oclusión y se notará.")]
        public LayerMask CapasQueOcluyen;

        [Header("Medidor de sospecha — APARTADO A DEL ENUNCIADO")]
        [Tooltip("Cuánto sube por segundo con evidencia máxima.")]
        public float VelocidadSubida = 0.8f;

        [Tooltip("Cuánto baja por segundo sin ver nada. Debe ser MUY inferior " +
                 "a la de subida: subir en 1-2 s, olvidar en 10-15 s.")]
        public float VelocidadBajada = 0.08f;

        [Tooltip("Umbral tranquilo → sospecha (al subir).")]
        [Range(0f, 1f)] public float UmbralSospecha = 0.35f;

        [Tooltip("Umbral sospecha → alerta (al subir).")]
        [Range(0f, 1f)] public float UmbralAlerta = 0.8f;

        [Tooltip("Cuánto hay que bajar por debajo del umbral para volver atrás. " +
                 "Sin esto el guardia parpadea entre estados. Es la HISTÉRESIS.")]
        [Range(0f, 0.5f)] public float Histeresis = 0.1f;

        [Header("Pesos de la evidencia")]
        public float PesoDistancia = 0.3f;
        public float PesoAngulo = 0.3f;
        public float PesoRayos = 0.4f;

        private static readonly Vector2[] PuntosObjetivo =
        {
            new Vector2(0f, 0.8f),
            new Vector2(0f, 0f),
            new Vector2(0f, -0.9f),
            new Vector2(-0.4f, 0.5f),
            new Vector2(0.4f, 0.5f)
        };

        // Estado interno del sensor. Lo publica en la pizarra al final de Percibir.
        private float _sospecha;
        private float _evidencia;
        private NivelSospecha _nivel = NivelSospecha.Tranquilo;
        private bool _visiblePrevio;

        private readonly Vector3[] _puntosMundo = new Vector3[PuntosObjetivo.Length];
        private readonly bool[] _rayoLibre = new bool[PuntosObjetivo.Length];
        private bool _rayosCalculados;

        /// <summary>Solo para los gizmos: si la última comprobación dio positivo.</summary>
        public bool VisibleAhora { get; private set; }
        public float SospechaActual { get { return _sospecha; } }
        public NivelSospecha NivelActual { get { return _nivel; } }

        private void Awake()
        {
            if (Ojos == null) Ojos = transform;

            if (Objetivo == null && !string.IsNullOrEmpty(TagObjetivo))
            {
                GameObject go = GameObject.FindGameObjectWithTag(TagObjetivo);
                if (go != null) Objetivo = go.transform;
            }
        }

        public void Percibir(Blackboard bb)
        {
            if (Objetivo == null) return;

            bool visible = ComprobarVision();
            VisibleAhora = visible;

            ActualizarSospecha(visible, Time.deltaTime);

            bb.Set(ClavesBB.Visible, visible);
            bb.Set(ClavesBB.Sospecha, _sospecha);
            bb.Set(ClavesBB.Nivel, _nivel);

            // La última posición conocida se escribe EN EL INSTANTE en que se
            // pierde el contacto. Ni antes (aún lo veo) ni después (ya no sé
            // dónde estaba).
            if (visible)
            {
                bb.Set(ClavesBB.UltimaPos, Objetivo.position);
                bb.Set(ClavesBB.TieneUltimaPos, true);
            }
            else if (_visiblePrevio)
            {
                bb.Set(ClavesBB.UltimaPos, Objetivo.position);
                bb.Set(ClavesBB.TieneUltimaPos, true);
                bb.Set(ClavesBB.VecesPerdido, bb.Get(ClavesBB.VecesPerdido, 0) + 1);
            }

            _visiblePrevio = visible;
        }

        private bool ComprobarVision()
        {
            _evidencia = 0f;
            _rayosCalculados = false;

            // ETAPA 1 · ALCANCE ✅
            // Comparamos distancias al cuadrado para ahorrarnos la raíz.
            Vector3 haciaObjetivo = Objetivo.position - Ojos.position;

            if (haciaObjetivo.sqrMagnitude > Alcance * Alcance) return false;

            float mitad = AnguloVision * 0.5f;
            float angulo = Vector3.Angle(Ojos.forward, haciaObjetivo);
            if (angulo > mitad) return false;

            Vector3 lado = Vector3.Cross(Vector3.up, haciaObjetivo).normalized;

            int llegan = 0;
            for (int i = 0; i < PuntosObjetivo.Length; i++)
            {
                Vector3 punto = Objetivo.position
                              + lado * PuntosObjetivo[i].x
                              + Vector3.up * PuntosObjetivo[i].y;
                Vector3 dir = punto - Ojos.position;
                bool libre = !Physics.Raycast(Ojos.position, dir.normalized, dir.magnitude, CapasQueOcluyen);

                _puntosMundo[i] = punto;
                _rayoLibre[i] = libre;
                if (libre) llegan++;
            }
            _rayosCalculados = true;

            if (llegan == 0) return false;

            float factorDistancia = 1f - haciaObjetivo.magnitude / Alcance;
            float factorAngulo = mitad > 0f ? 1f - angulo / mitad : 1f;
            float factorRayos = llegan / (float)PuntosObjetivo.Length;

            _evidencia = Mathf.Clamp01(PesoDistancia * factorDistancia
                                     + PesoAngulo * factorAngulo
                                     + PesoRayos * factorRayos);
            return true;
        }

        private void ActualizarSospecha(bool visible, float dt)
        {
            if (visible) _sospecha += _evidencia * VelocidadSubida * dt;
            else _sospecha -= VelocidadBajada * dt;

            _sospecha = Mathf.Clamp01(_sospecha);

            _nivel = ActualizarNivel(_sospecha, _nivel);
        }

        private void OnDrawGizmos()
        {
            if (!_rayosCalculados || Ojos == null) return;

            for (int i = 0; i < _puntosMundo.Length; i++)
            {
                Gizmos.color = _rayoLibre[i] ? Color.green : Color.red;
                Gizmos.DrawLine(Ojos.position, _puntosMundo[i]);
            }
        }

        /// <summary>
        /// Umbrales con histéresis: para SUBIR de nivel hace falta pasar el
        /// umbral; para BAJAR hace falta caer por debajo del umbral MENOS la
        /// histéresis. Sin esto, un valor oscilando alrededor de 0,35 hace que
        /// el guardia entre y salga de sospecha varias veces por segundo, y en
        /// pantalla parece que le da un ataque.
        ///
        /// Este método OS LO DOY HECHO porque es fácil equivocarse y no es lo
        /// interesante. Lo interesante es el medidor de arriba.
        /// </summary>
        private NivelSospecha ActualizarNivel(float s, NivelSospecha actual)
        {
            switch (actual)
            {
                case NivelSospecha.Tranquilo:
                    if (s >= UmbralSospecha) return NivelSospecha.Sospecha;
                    break;

                case NivelSospecha.Sospecha:
                    if (s >= UmbralAlerta) return NivelSospecha.Alerta;
                    if (s < UmbralSospecha - Histeresis) return NivelSospecha.Tranquilo;
                    break;

                case NivelSospecha.Alerta:
                    if (s < UmbralAlerta - Histeresis) return NivelSospecha.Sospecha;
                    break;
            }
            return actual;
        }
    }
}