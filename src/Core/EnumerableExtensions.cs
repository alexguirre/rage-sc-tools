namespace ScTools;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    public static IEnumerable<TSource> AppendIfNotNull<TSource>(this IEnumerable<TSource> source, TSource? element)
        => element is null ? source : source.Append(element);
    public static IEnumerable<TSource> AppendIfNotNull<TSource>(this IEnumerable<TSource> source, TSource? element) where TSource : struct
        => element is null ? source : source.Append(element.Value);
}
