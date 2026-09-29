

namespace Monochrome
{
	/// <summary>
	/// 模组主类。按蓝图约定，这里<b>只做装配</b>，不放任何玩法逻辑。
	/// </summary>
	public class Monochrome : Mod
	{
		/// <summary>当前模组实例的快捷访问点。</summary>
		public static Monochrome Instance;

		/// <inheritdoc/>
		public override void Load()
		{
			Instance = this;
		}
	}
}
