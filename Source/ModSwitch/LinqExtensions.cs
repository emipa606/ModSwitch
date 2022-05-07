using System;
using System.Collections.Generic;
using System.Linq;

namespace DoctorVanGogh.ModSwitch;

internal static class LinqExtensions
{
    internal static IEnumerable<TResult> FullOuterGroupJoin<TA, TB, TKey, TResult>(this IEnumerable<TA> a,
        IEnumerable<TB> b, Func<TA, TKey> selectKeyA, Func<TB, TKey> selectKeyB,
        Func<IEnumerable<TA>, IEnumerable<TB>, TKey, TResult> projection, IEqualityComparer<TKey> cmp = null)
    {
        if (cmp == null)
        {
            cmp = EqualityComparer<TKey>.Default;
        }

        var alookup = a.ToLookup(selectKeyA, cmp);
        var blookup = b.ToLookup(selectKeyB, cmp);
        var hashSet = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
        hashSet.UnionWith(blookup.Select(p => p.Key));
        return from key in hashSet
            let xa = alookup[key]
            let xb = blookup[key]
            select projection(xa, xb, key);
    }

    internal static IEnumerable<TResult> FullOuterJoin<TA, TB, TKey, TResult>(this IEnumerable<TA> a, IEnumerable<TB> b,
        Func<TA, TKey> selectKeyA, Func<TB, TKey> selectKeyB, Func<TA, TB, TKey, TResult> projection,
        TA defaultA = default, TB defaultB = default, IEqualityComparer<TKey> cmp = null)
    {
        if (cmp == null)
        {
            cmp = EqualityComparer<TKey>.Default;
        }

        var alookup = a.ToLookup(selectKeyA, cmp);
        var blookup = b.ToLookup(selectKeyB, cmp);
        var hashSet = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
        hashSet.UnionWith(blookup.Select(p => p.Key));
        return from key in hashSet
            from xa in alookup[key].DefaultIfEmpty(defaultA)
            from xb in blookup[key].DefaultIfEmpty(defaultB)
            select projection(xa, xb, key);
    }
}