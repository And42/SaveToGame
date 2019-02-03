﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class CollectionUtils
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T item in collection)
                action(item);
        }

        public static string JoinStr(this IEnumerable<string> elements, string separator)
        {
            return string.Join(separator, elements);
        }

        public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> collection)
        {
            return collection.Select((value, index) => (index, value));
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> collection) where T : class
        {
            return collection.Where(it => it != null);
        }

        public static IEnumerable<T> WhereNotNull<T, R>(this IEnumerable<T> collection, Func<T, R> selector) where R : class
        {
            return collection.Where(it => selector(it) != null);
        }

        public static IEnumerable<(TKey key, TValue value)> Enumerate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary
        )
        {
            foreach (var item in dictionary)
                yield return (item.Key, item.Value);
        }
    }
}
