namespace UDIT.IA3.Guardia
{
    public class EstadoPerseguir : State
    {
        public EstadoPerseguir(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            PedirUltimaPos();
            Pizarra.Set(ClavesBB.HaLlegado, false);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = Pizarra.Get(ClavesBB.Visible, false);

            if (nivel == NivelSospecha.Alerta && visible)
            {
                PedirUltimaPos();
                return;
            }

            if (nivel == NivelSospecha.Sospecha)
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
            else if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
            else
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }

        private void PedirUltimaPos()
        {
            Pizarra.Set(ClavesBB.Destino, Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position));
            Pizarra.Set(ClavesBB.TieneDestino, true);
        }
    }
}