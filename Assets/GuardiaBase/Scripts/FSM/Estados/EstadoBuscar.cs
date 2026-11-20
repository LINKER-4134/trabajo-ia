namespace UDIT.IA3.Guardia
{
    public class EstadoBuscar : State
    {
        private const float EsperaSegundos = 1;
        private bool _esperando;
        private float _fin;

        public EstadoBuscar(StateMachine machine) : base(machine) { }

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
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }
            if (nivel == NivelSospecha.Sospecha)
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            if (!_esperando)
            {
                if (Pizarra.Get(ClavesBB.HaLlegado, false))
                {
                    _esperando = true;
                    _fin = Pizarra.Tiempo + EsperaSegundos;
                    Pizarra.Set(ClavesBB.TieneDestino, false);
                }
                return;
            }

            if (Pizarra.Tiempo >= _fin)
            {
                Pizarra.Set(ClavesBB.TieneUltimaPos, false);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
            }
        }
    }
}