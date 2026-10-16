using ReLogic.Utilities;
using Terraria.Audio;

namespace Monochrome.Core.Sounds;

/// <summary>
/// 一段循环播放的声音，位置每帧跟着声源走。
/// <para>
/// 只应由 <see cref="MonoLoopedSoundManager.CreateNew"/> 创建：管理器负责在它被原版掐掉后重排，
/// 以及按终止条件或 <see cref="Stop"/> 把它注销。
/// </para>
/// <para>
/// 消费者每帧调一次 <see cref="Update"/> 就够了，不需要自己管起播与位置。
/// </para>
/// </summary>
public class MonoLoopedSound
{
    /// <summary>要循环的那个声音。</summary>
    protected readonly SoundStyle LoopSoundStyle;

    /// <summary>
    /// 自动终止条件，每帧评估一次，为真就停掉并注销。它是"忘了停"的兜底：
    /// 挂在实体上的循环音应当在实体消失时自己结束。
    /// </summary>
    public Func<bool> AutomaticTerminationCondition { get; protected set; }

    /// <summary>这段声音在 <see cref="SoundEngine"/> 里的槽位。</summary>
    public SlotId LoopingSoundSlot { get; protected set; } = SlotId.Invalid;

    /// <summary>是否已经起播过。</summary>
    public bool HasLoopSoundBeenStarted { get; protected set; }

    /// <summary>是否已被 <see cref="Stop"/> 停掉。</summary>
    public bool HasBeenStopped { get; internal set; }

    /// <summary>这一段现在是否真的在响。</summary>
    public bool LoopIsBeingPlayed => SoundEngine.TryGetActiveSound(LoopingSoundSlot, out _);

    internal MonoLoopedSound(SoundStyle loopingSound, Func<bool> automaticTerminationCondition)
    {
        LoopSoundStyle = loopingSound;
        AutomaticTerminationCondition = automaticTerminationCondition;
    }

    /// <summary>
    /// 每帧调一次：没起播就起播，起播了就更新位置，并把 <paramref name="soundUpdateStep"/> 作用到活动声音上。
    /// </summary>
    /// <param name="soundPosition">这一帧的声源位置。</param>
    /// <param name="soundUpdateStep">可选的声音微调（音量、音调、跟随等）。</param>
    public virtual void Update(Vector2 soundPosition, Action<ActiveSound>? soundUpdateStep = null)
    {
        // 专用服务器上没有声音设备；PlaySound 在这里没有意义，也没有客户端会听。
        if (Main.dedServ)
            return;

        if (!HasLoopSoundBeenStarted && !LoopIsBeingPlayed)
            StartSounds(soundPosition);

        UpdateSoundSlots(soundPosition, soundUpdateStep);
    }

    /// <summary>
    /// 把"已停"与"已起播"两个标记都清掉，让它在下一次 <see cref="Update"/> 时重新起播。
    /// </summary>
    public void Restart()
    {
        HasLoopSoundBeenStarted = false;
        HasBeenStopped = false;
    }

    /// <summary>停掉并注销。重复调用是安全的。</summary>
    public void Stop()
    {
        if (HasBeenStopped)
            return;

        StopSoundsInternal();
        HasBeenStopped = true;
    }

    /// <summary>起播循环音。<see cref="Update"/> 与管理器都会用到。</summary>
    /// <param name="soundPosition">声源位置。</param>
    protected virtual void StartSounds(Vector2 soundPosition)
    {
        LoopingSoundSlot = SoundEngine.PlaySound(LoopSoundStyle with
        {
            // 循环音不能被实例上限掐掉，也不该让原版按播放次数结束它。
            MaxInstances = 0,
            IsLooped = true,
        }, soundPosition);
        HasLoopSoundBeenStarted = true;
    }

    /// <summary>
    /// 把位置与可选微调写进活动声音；槽位没了就把"已起播"标回 false，等下一次 <see cref="Update"/> 重来。
    /// </summary>
    /// <param name="soundPosition">声源位置。</param>
    /// <param name="soundUpdateStep">可选的声音微调。</param>
    protected virtual void UpdateSoundSlots(Vector2 soundPosition, Action<ActiveSound>? soundUpdateStep = null)
    {
        if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound? sound) && sound is not null)
        {
            sound.Position = soundPosition;
            soundUpdateStep?.Invoke(sound);
        }
        else if (!HasBeenStopped)
        {
            HasLoopSoundBeenStarted = false;
        }
    }

    /// <summary>只做"停"，不改状态位。</summary>
    protected virtual void StopSoundsInternal()
    {
        if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound? sound))
            sound?.Stop();
    }
}
