using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveToGameWpf.Logic.Utils
{
    public static class CollectionUtils
    {
        public static IEnumerable<(int index, T value)> WithIndex<T>(this IEnumerable<T> collection)
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
    }
}
