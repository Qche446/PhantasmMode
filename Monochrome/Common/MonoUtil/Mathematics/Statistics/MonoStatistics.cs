
namespace Monochrome.Common.MonoUtil
{
    public static partial class MonoUtil
    {
        /// <summary>
        /// 计算整数的整数幂次,power得为自然数,若小于0返回1
        /// </summary>
        public static int IntPow(int num, int power)
        {
            int result = 1;
            for (int i = 0; i < power; i++)
            {
                result *= num;
            }
            return result;
        }
        /// <summary>
        /// 阶乘，N不要小于0,也不要太大
        /// </summary>
        public static long Factorial(int N)
        {
            if (N == 0)
                return 1;
            int result = 1;
            for (int i = 1; i <= N; i++)
                result *= i;
            return result;
        }
        /// <summary>
        /// 排列数，m不要大于n
        /// </summary>
        public static long Permutation(int n, int m) => Factorial(n) / Factorial(n - m);
        /// <summary>
        /// 组合数，m不要大于n
        /// </summary>
        public static long Combinatorial(int n, int m) => Permutation(n, m) / Factorial(m);
    }
}
