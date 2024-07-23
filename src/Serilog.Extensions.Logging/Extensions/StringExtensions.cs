using System.Runtime.CompilerServices;

namespace Serilog.Extensions;

#if !NET6_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWith(this string str, char value)
    {
        return str.Length > 0 && str[0] == value;
    }
}
#endif
