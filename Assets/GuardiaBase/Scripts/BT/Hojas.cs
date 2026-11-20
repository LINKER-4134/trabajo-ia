using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public sealed class Patrullar : Node
    {
        private readonly GuardiaAgente _agente;

        public Patrullar(GuardiaAgente agente) : base("Patrullar") { _agente = agente; }

        protected override void OnInitialize(Blackboard bb)
        {
            RutaPatrulla ruta = _agente.GetComponent<RutaPatrulla>();
            if (ruta != null && ruta.TieneRuta)
            {
                bb.Set(ClavesBB.Destino, ruta.PuntoActual);
                bb.Set(ClavesBB.TieneDestino, true);
                bb.Set(ClavesBB.HaLlegado, false);
            }
        }

        protected override Status Update(Blackboard bb)
        {
            RutaPatrulla ruta = _agente.GetComponent<RutaPatrulla>();
            if (ruta == null || !ruta.TieneRuta) return Status.Failure;

            if (bb.Get(ClavesBB.HaLlegado, false))
            {
                bb.Set(ClavesBB.Destino, ruta.Avanzar());
                bb.Set(ClavesBB.TieneDestino, true);
            }
            return Status.Running;
        }
    }

    public sealed class Perseguir : Node
    {
        public Perseguir() : base("Perseguir") { }

        protected override void OnInitialize(Blackboard bb)
        {
            bb.Set(ClavesBB.HaLlegado, false);
        }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.Destino, bb.Get(ClavesBB.UltimaPos, Vector3.zero));
            bb.Set(ClavesBB.TieneDestino, true);
            return Status.Running;
        }
    }

    public sealed class Investigar : Node
    {
        public Investigar() : base("Investigar") { }

        protected override void OnInitialize(Blackboard bb)
        {
            bb.Set(ClavesBB.Destino, bb.Get(ClavesBB.UltimaPos, Vector3.zero));
            bb.Set(ClavesBB.TieneDestino, true);
            bb.Set(ClavesBB.HaLlegado, false);
        }

        protected override Status Update(Blackboard bb)
        {
            return Status.Running;
        }
    }

    public sealed class IrA : Node
    {
        private readonly string _clave;

        public IrA(string nombre, string clave) : base(nombre) { _clave = clave; }

        protected override void OnInitialize(Blackboard bb)
        {
            bb.Set(ClavesBB.Destino, bb.Get(_clave, Vector3.zero));
            bb.Set(ClavesBB.TieneDestino, true);
            bb.Set(ClavesBB.HaLlegado, false);
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Get(ClavesBB.HaLlegado, false) ? Status.Success : Status.Running;
        }
    }

    public sealed class Esperar : Node
    {
        private readonly float _segundos;
        private float _fin;

        public Esperar(float segundos) : base("Esperar " + segundos + "s") { _segundos = segundos; }

        protected override void OnInitialize(Blackboard bb)
        {
            _fin = bb.Tiempo + _segundos;
            bb.Set(ClavesBB.TieneDestino, false);
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Tiempo >= _fin ? Status.Success : Status.Running;
        }
    }

    public sealed class Olvidar : Node
    {
        public Olvidar() : base("Olvidar") { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneUltimaPos, false);
            return Status.Success;
        }
    }
}