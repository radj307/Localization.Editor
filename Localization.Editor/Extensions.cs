using System;
using System.Collections.Generic;

namespace Localization.Editor
{
    static class Extensions
    {
        public static T GetOrCreate<T>(this IList<T> list, Func<T, bool> predicate, Func<T> itemFactory)
        {
            for (int i = 0, i_max = list.Count; i < i_max; ++i)
            {
                var item = list[i];
                if (predicate(item))
                    return item;
            }
            var newItem = itemFactory();
            list.Add(newItem);
            return newItem;
        }
        public static T GetOrCreate<T>(this IList<T> list, Func<T, bool> predicate) where T : new()
            => GetOrCreate(list, predicate, () => new());
    }
}
