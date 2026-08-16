using Luminance.Common.StateMachines;
using System;

namespace FargosPhantasmMode.Common
{
    public class Pstate<TStateIdentifier>(TStateIdentifier identifier) : IState<TStateIdentifier> where TStateIdentifier : struct
    {
        public TStateIdentifier Identifier { get; set; } = identifier;
        public float[] ai = new float[4];
        public void OnPopped()
        {
            Array.Clear(ai, 0, ai.Length);
        }
    }
}
