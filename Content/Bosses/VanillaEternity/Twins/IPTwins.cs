using System.Collections.Generic;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public interface IPTwins
    {
        public int Phase { get; set; }
        public int Phaseinit { get; set; }
        public TwinsAtt AIState { get; set; }
        public bool Ignite { get; set; }
        public bool isDeathray { get; set; }
        public List<TwinsAtt> Phase1 { get;}
        public List<TwinsAtt> Phase2 { get;}
        public List<TwinsAtt> Phase3 { get;}
        public int OrbColor { get;}
    }
    public enum TwinsAtt
    {
        //P1
        PhaseChange1st,
        //P2
        PhaseChange2nd,
        //P3
        //
        LocateAndWaitDash,
        RotatedAndWaitDash,
        Deathray,

    }
}

