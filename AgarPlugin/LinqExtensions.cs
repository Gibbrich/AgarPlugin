using System;
using System.Collections.Generic;

namespace AgarPlugin
{
    public static class CollectionsExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);                
            }
        }
    }
}