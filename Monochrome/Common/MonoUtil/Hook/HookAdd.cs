using MonoMod.Cil;

namespace Monochrome.Common.MonoUtil
{
    public static partial class MonoUtil
    {
        /// <summary>
        /// 获取模组类实例的简写
        /// </summary>
        public static IT ModInstance<IT>() where IT : class => ModContent.GetInstance<IT>();
        /// <summary>
        /// 添加委托钩子的简写。静态方法直接写出来，若目标方法本身为非静态，则先获取实例后再调用
        /// </summary>
        /// <typeparam name="T">目标方法的方法签名</typeparam>
        /// <typeparam name="K">你要挂上的方法签名</typeparam>
        /// <param name="a">目标方法的方法签名</param>
        /// <param name="b">你要挂上的方法签名</param>
        public static void AddHooks<T, K>(T a, K b) where T : Delegate where K : Delegate => MonoModHooks.Add(a.Method, b);
        /// <summary>
        /// 添加IL钩子的简写。静态方法直接写出来，若目标方法本身为非静态，则先获取实例后再调用
        /// </summary>
        public static void AddILHooks<T>(T a, ILContext.Manipulator b) where T : Delegate => MonoModHooks.Modify(a.Method, b);
    }
}
