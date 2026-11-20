namespace UDIT.IA3.Guardia
{
    public class EstadoInvestigar : State
    {
        public EstadoInvestigar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            Pizarra.Set(ClavesBB.Destino, Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position));
            Pizarra.Set(ClavesBB.TieneDestino, true);
            Pizarra.Set(ClavesBB.HaLlegado, false);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = Pizarra.Get(ClavesBB.Visible, false);

            if (nivel == NivelSospecha.Alerta && visible)
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
            else if (nivel != NivelSospecha.Sospecha)
            {
                if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
                    StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                else
                    StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
            }
        }
    }
}