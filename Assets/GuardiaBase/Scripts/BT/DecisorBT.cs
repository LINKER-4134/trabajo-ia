// ═══════════════════════════════════════════════════════════════════════════
//  DecisorBT.cs — Cerebro versión 2: árbol de comportamiento
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  APARTADO C DEL ENUNCIADO.
//
//  El runtime (BehaviorTree.cs) OS LO DOY. No lo modifiquéis: en la sesión 9
//  lo explicamos línea a línea y en la memoria os preguntaré por él.
//
//  Lo que escribís vosotros es lo de este fichero: LA FORMA DEL ÁRBOL y LOS
//  NODOS HOJA. Eso es lo que se evalúa.
//
//  ⚠️ La condición del apartado D: este decisor tiene que producir el MISMO
//  comportamiento observable que DecisorFSM, leyendo la MISMA pizarra, sin que
//  haga falta tocar ni SensorVision ni LocomocionNavMesh. Si para que funcione
//  el árbol necesitáis cambiar algo de la percepción, algo está mal colocado.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorBT : IDecisor
    {
        private readonly GuardiaAgente _agente;
        private BehaviorTree _arbol;

        public DecisorBT(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            _arbol = new BehaviorTree(ConstruirArbol(), bb);
        }

        public void Decidir(Blackboard bb)
        {
            if (_arbol != null) _arbol.Tick();
        }

        public string Traza { get { return "BT: " + Trazador.Ultima(); } }

        private Node ConstruirArbol()
        {
            return new ActiveSelector("Guardia",

                new Monitor("SiAlerta",
                    new Condition("¿Alerta y visible?", bb =>
                        bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Alerta
                        && bb.Get(ClavesBB.Visible, false)),
                    new Perseguir()),

                new Monitor("SiSospecha",
                    new Condition("¿Sospecha?", bb =>
                        bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Sospecha),
                    new Investigar()),

                new Sequence("Buscar",
                    new Condition("¿Hay última pos?", bb =>
                        bb.Get(ClavesBB.TieneUltimaPos, false)),
                    new IrA("IrAUltimaPos", ClavesBB.UltimaPos),
                    new Esperar(1f),
                    new Olvidar()),

                new Patrullar(_agente)
            );
        }
    }
}