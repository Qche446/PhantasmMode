namespace FargosPhantasmMode.Content.Bosses.Abom
{
    internal enum AbomAttackState
    {
        ActuallyDead,

        PhaseChange1st,
        ThrowScythes,
        FlamingScytheSpread,
        PhoenixDash,
        ChooseNextAttack1st,
        SicklePhalanx, CirnoIcicle, SaucerRockets,
        BloodNeedle, ShadowScycle,
        ManeuverScycle, PreDeathRain1st, DeathraysDash1st, PreDeathrain2nd, DeathraysDash2nd, PauseToPre,
        LaevateinnSword, laevateinnDash, WaitScythesClear, VerticalLaevateinn, VerticalDash,

        PhaseChange2nd,
        Final_ThrowScythes, Final_Laevateinn, 
    }
}
