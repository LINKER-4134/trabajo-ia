// ═══════════════════════════════════════════════════════════════════════════
//  EstadoPatrullar.cs — EJEMPLO COMPLETO del patrón Estado
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  Éste OS LO DOY ENTERO, para que veáis la forma. Los otros tres estados
//  —Investigar, Perseguir, Buscar— los escribís vosotros copiando esta
//  estructura.
//
//  Fijaos en las tres cosas que hace y en las tres que NO hace:
//
//    SÍ:  lee la pizarra · escribe intenciones (destino) · transita
//    NO:  mirar el mundo (eso es del sensor)
//         llamar al NavMeshAgent (eso es de LocomocionNavMesh)
//         guardar «en qué estado estoy» en la pizarra (para eso está la FSM)
// ═══════════════════════════════════════════════════════════════════════════

namespace UDIT.IA3.Guardia
{
    public class EstadoPatrullar : State
    {
        public EstadoPatrullar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();
            if (ruta != null && ruta.TieneRuta)
            {
                Pizarra.Set(ClavesBB.Destino, ruta.PuntoActual);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);
            }
            else
            {
                Pizarra.Set(ClavesBB.TieneDestino, false);
                UnityEngine.Debug.LogWarning(
                    "[" + Agente.name + "] no tiene RutaPatrulla con puntos. " +
                    "Mirad los apartados 3.4 y 3.5 del README_Montaje.", Agente);
            }
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = Pizarra.Get(ClavesBB.Visible, false);

            if (nivel == NivelSospecha.Alerta && visible)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }
            if (nivel == NivelSospecha.Sospecha)
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }
            if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            if (Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();
                if (ruta != null && ruta.TieneRuta)
                {
                    Pizarra.Set(ClavesBB.Destino, ruta.Avanzar());
                    Pizarra.Set(ClavesBB.TieneDestino, true);
                }
            }
        }

        internal override void Exit() { }
    }
}
