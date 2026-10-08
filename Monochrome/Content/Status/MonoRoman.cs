using System.Globalization;

namespace Monochrome.Content.Status;

/// <summary>
/// 罗马数字的转换，给层数角标这类小字用。
/// <para>
/// 只认 1 到 3999 的标准写法；超出这个范围回落到阿拉伯数字，因为再往上（MMMM…）反而更难读。
/// 这是个纯函数，能直接进离线验收台。
/// </para>
/// </summary>
public static class MonoRoman
{
    /// <summary>从大到小的值表，贪心取值即可得到标准写法。</summary>
    private static readonly (int Value, string Text)[] Table =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
        (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    ];

    /// <summary>把整数写成罗马数字。</summary>
    /// <param name="value">要转换的值。</param>
    /// <returns>1 到 3999 之间的标准写法；该范围之外返回阿拉伯数字，0 与负数返回空串。</returns>
    public static string ToRoman(int value)
    {
        if (value <= 0)
            return string.Empty;

        if (value > 3999)
            return value.ToString(CultureInfo.InvariantCulture);

        // 最长的是 3888（MMMDCCCLXXXVIII），15 个字符。
        Span<char> buffer = stackalloc char[16];
        int length = 0;

        foreach ((int digit, string text) in Table)
        {
            while (value >= digit)
            {
                text.AsSpan().CopyTo(buffer[length..]);
                length += text.Length;
                value -= digit;
            }
        }

        return new string(buffer[..length]);
    }
}
