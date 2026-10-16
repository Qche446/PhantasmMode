using Terraria.Audio;

namespace Monochrome.Core.Sounds;

/// <summary>
/// 循环音的登记处：每帧扫一遍在册的声音，把被原版掐掉的重新排上，把满足终止条件或已显式停掉的注销。
/// <para>
/// 用 <see cref="CreateNew"/> 建出来的实例才归它管；注销之后实例本身不会被释放，但已经停了。
/// </para>
/// </summary>
public sealed class MonoLoopedSoundManager : ModSystem
{
    /// <summary>当前在册的循环音。</summary>
    private static readonly List<MonoLoopedSound> loopedSounds = [];

    /// <summary>当前在册的循环音数量。</summary>
    public static int Count => loopedSounds.Count;

    /// <summary>
    /// 建一段循环音并登记。
    /// </summary>
    /// <param name="loopingSound">要循环的声音。</param>
    /// <param name="automaticTerminationCondition">
    /// 自动终止条件，每帧评估；为 <see langword="null"/> 表示永不自终止，只能靠显式 <see cref="MonoLoopedSound.Stop"/>。
    /// </param>
    public static MonoLoopedSound CreateNew(SoundStyle loopingSound, Func<bool>? automaticTerminationCondition = null)
    {
        MonoLoopedSound sound = new(loopingSound, automaticTerminationCondition ?? (() => false));
        loopedSounds.Add(sound);
        return sound;
    }

    /// <inheritdoc/>
    public override void Load()
    {
        if (!Main.dedServ)
            On_SoundEngine.Update += UpdateLoopedSounds;
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        if (!Main.dedServ)
            On_SoundEngine.Update -= UpdateLoopedSounds;
        loopedSounds.Clear();
    }

    /// <summary>
    /// 扫描在册的循环音，并<b>始终</b>把 <paramref name="orig"/> 调下去——
    /// 这个钩子包着原版的整个声音更新，漏调会让所有声音失效。
    /// </summary>
    private void UpdateLoopedSounds(On_SoundEngine.orig_Update orig)
    {
        if (SoundEngine.IsAudioSupported)
        {
            // 倒序遍历：注销会改变后面的索引。
            for (int i = loopedSounds.Count - 1; i >= 0; i--)
            {
                MonoLoopedSound sound = loopedSounds[i];

                // 起播过却不在响了（切场景时原版会清空声音），重排一次。
                if (sound.HasLoopSoundBeenStarted && !sound.LoopIsBeingPlayed)
                    sound.Restart();

                if (sound.AutomaticTerminationCondition() || sound.HasBeenStopped)
                {
                    sound.Stop();
                    loopedSounds.RemoveAt(i);
                }
            }
        }

        orig();
    }
}
