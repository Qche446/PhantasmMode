using System.Collections.Generic;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public interface IPTwins
    {
        public int Phase { get; set; }
        public int Phaseinit { get; set; }
        public TwinsAtt AIState { get; set; }
        public bool Ignite { get; set; }
        public int IgniteTimer { get; set; }
        public bool Ghost { get; set; }
        public List<TwinsAtt> Phase1 { get;}
        public List<TwinsAtt> Phase2 { get;}
        public List<TwinsAtt> Phase3 { get;}
        public int OrbColor { get;}
    }
    public enum TwinsAtt
    {
        //P1
        PhaseChange1st,
        NormalShoot, FlankingShoot,
        CurFireDash, LegFireDash, P1_BreathedFire,


        //P2
        PhaseChange2nd,
        SineShoot, PolyRing, CurvedDeathRay,
        P2_BreathedFire, RollingShoot,


        //P3
        PhaseChange3rd,
        LocatedShoot,
        Final_PolyRing, FireRotate,
        BulletHell_Open, Final_LegFireDash, 
        BulletHell_End, Final_CurFireDashBreathed,
        Final_Deathray, Final_Embers,
    }
}

