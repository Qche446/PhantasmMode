namespace Monochrome.Core.Net;

/// <summary>
/// 请求的非泛型视图：路由只需要知道 id 与"怎么读载荷并处理"。
/// </summary>
public abstract class MonoNetRequestBase
{
    private protected MonoNetRequestBase(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>写进包里的请求 id，由名字算出来。</summary>
    public ushort Id { get; }

    /// <summary>请求名，同时参与 id 与协议表哈希的计算。</summary>
    public string Name { get; }

    /// <summary>从包体读载荷并交给复核与执行。读失败会抛，由收包处兜住并计数。</summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号，由传输层给出。</param>
    internal abstract void ApplyFromNetwork(BinaryReader reader, int whoAmI);

    /// <summary>诊断输出。</summary>
    public abstract string Describe();
}

/// <summary>
/// 一条客户端→服务端的请求：客户端只提交意图，服务端复核之后才执行。
/// <para>
/// 这类交互里最容易漏的三件事被收进了模板：<b>①</b> 发送者身份由传输层给出（<c>whoAmI</c>），
/// 不再由客户端在包里自报，于是载荷里那个"我是谁"的字段可以直接删掉；<b>②</b> 服务端必须显式复核——
/// <see cref="Validate"/> 没挂时<b>一律拒绝</b>，忘了写校验不等于放行；<b>③</b> 复核不通过、没有处理者、
/// 未知 id、载荷读不出来、位置不对（客户端收到上行请求或服务端自己发），全都计数并记日志。
/// </para>
/// <para>
/// <b>单机下 <see cref="Send"/> 直接在本机走一遍复核与执行</b>，所以消费者不必再写"单机分支"。
/// </para>
/// <para>
/// 它只负责"把请求送到服务端并交给复核"。要用请求改变世界状态，处理体里写一个
/// <see cref="MonoNet.WorldFlag{T}"/> 就够了——那条路自己会下发。
/// </para>
/// </summary>
/// <typeparam name="T">载荷类型。</typeparam>
public sealed class MonoNetRequest<T> : MonoNetRequestBase where T : struct
{
    private readonly Action<BinaryWriter, T> write;
    private readonly Func<BinaryReader, T> read;

    internal MonoNetRequest(ushort id, string name, Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
        : base(id, name)
    {
        this.write = write;
        this.read = read;
    }

    /// <summary>
    /// 服务端的复核。参数是<b>真实的发送者编号</b>与载荷；返回 false 就丢弃这一条（计数 + 记日志）。
    /// <para>
    /// <b>没挂它时一律拒绝。</b>确实要"来者不拒"就显式写 <c>Validate = static (_, _) =&gt; true;</c>——
    /// 那是有意为之的决定，应该看得见。
    /// </para>
    /// </summary>
    public Func<int, T, bool>? Validate { get; set; }

    /// <summary>复核通过之后在服务端执行。没挂它时什么都不做，但会计一次"没有处理者"。</summary>
    public Action<int, T>? OnServer { get; set; }

    /// <summary>
    /// 客户端提交载荷。单机直接在本机复核并执行；多人由客户端发给服务端；
    /// <b>在服务端调用会被拒绝并计数</b>（请求是上行通道）。
    /// </summary>
    /// <param name="payload">载荷。</param>
    public void Send(T payload) => MonoNet.SendRequest(this, payload);

    /// <inheritdoc/>
    public override string Describe() => $"#{Id} 「{Name}」";

    internal void WritePayload(BinaryWriter writer, T payload) => write(writer, payload);

    internal override void ApplyFromNetwork(BinaryReader reader, int whoAmI)
        => MonoNet.ApplyRequest(this, whoAmI, read(reader));
}
