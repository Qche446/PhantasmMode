using System.IO;
using Monochrome.Core.Graphics.Shaders;
using Monochrome.Core.Net;
using Monochrome.Core.Services;

namespace Monochrome
{
	/// <summary>
	/// 模组主类。
	/// </summary>
	public class Monochrome : Mod
	{
		/// <summary>当前模组实例的快捷访问点。仅在 <see cref="Load"/> 之后非 null，访问前请用 <c>?.</c>。</summary>
		public static Monochrome? Instance;

		/// <inheritdoc/>
		public override void Load()
		{
			Instance = this;

			// 实验
			//MonoServiceHost.Register<MonoLifecycleProbe>();
		}

		/// <summary>
		/// Monochrome 自己的包。消息 id 是<b>本模组的包空间</b>：所有消费者共用它，
		/// 所以新增消息要在 <see cref="MonoNetSystem"/> 里登记一个常量，并在这里接上分支。
		/// </summary>
		/// <param name="reader">包体，已经跳过 mod id。</param>
		/// <param name="whoAmI">发送者编号。</param>
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			byte messageId = reader.ReadByte();
			switch (messageId)
			{
				case MonoNetSystem.MsgWorldFlags:
					MonoNetSystem.HandleWorldFlags(reader, whoAmI);
					break;

				case MonoNetSystem.MsgPing:
					MonoNetSystem.HandlePing(reader, whoAmI);
					break;

				case MonoNetSystem.MsgPong:
					MonoNetSystem.HandlePong(reader);
					break;

				case MonoNetSystem.MsgRequest:
					MonoNetSystem.HandleRequest(reader, whoAmI);
					break;

				default:
					MonoNet.NoteUnknownMessage(messageId, whoAmI);
					break;
			}
		}

		/// <summary>
		/// 在所有模组的内容都注册完之后，把依赖本模组的着色器资产扫进
		/// <see cref="MonoShaderManager"/>，然后排空等待队列。
		/// <para>
		/// 必须等到 <c>PostSetupContent</c>：<c>ModContent.Request</c> 要求资产的注册表已经建立。
		/// 专用服务器上没有图形设备，直接跳过。
		/// </para>
		/// </summary>
		public override void PostSetupContent()
		{
			if (Main.dedServ)
				return;
			foreach (Mod mod in ModLoader.Mods)
				MonoShaderManager.LoadForMod(mod);
			MonoShaderManager.HasFinishedLoading = true;
			while (MonoShaderManager.PostShaderLoadActions.TryDequeue(out Action? action))
				action?.Invoke();

			// Luminance 的 ShaderRecompilationMonitor 在它自己的 PostSetupContent 里建好文件监视器；
			// Monochrome 的 sortAfter = Luminance，所以走到这里时它的监视器已经就位，可以安全停用。
			MonoShaderReloader.TrySuppressLuminanceAutoReload();
		}
	}
}
