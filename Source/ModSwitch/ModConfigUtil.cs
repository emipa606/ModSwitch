using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DoctorVanGogh.ModSwitch;

public static class ModConfigUtil
{
    public static string GetConfigFilename(string foldername, string modClassName)
    {
        return $"Mod_{foldername}_{modClassName}.xml";
    }

    public static string GetConfigFilename(this ModMetaData md, string modClassName)
    {
        return GetConfigFilename(md.FolderName, modClassName);
    }

    public static (TResult[] Resolved, TResult[] Unresolved) TryResolveModsList<T, TKey, TResult>(
        IEnumerable<T> candidates, Func<ModMetaData, TKey> installedKeyFactory, Func<T, TKey> candidateKeyFactory,
        Func<ModMetaData, T, TResult> resultFactory)
    {
        return TryResolveModsList(candidates, installedKeyFactory, candidateKeyFactory, resultFactory, resultFactory);
    }

    public static (TResolved[] Resolved, TUnresolved[] Unresolved) TryResolveModsList<T, TKey, TResolved, TUnresolved>(
        IEnumerable<T> candidates, Func<ModMetaData, TKey> installedKeyFactory, Func<T, TKey> candidateKeyFactory,
        Func<ModMetaData, T, TResolved> resolvedProjection, Func<ModMetaData, T, TUnresolved> unresolvedProjection)
    {
        var dictionary =
            (from t in LetOuterJoin(candidates, installedKeyFactory, candidateKeyFactory)
                group t by t.MetaData == null)
            .ToDictionary(g => g.Key);
        dictionary.TryGetValue(false, out var value);
        dictionary.TryGetValue(true, out var value2);
        return (
            value?.Select<(TKey, ModMetaData, T), TResolved>(((TKey Key, ModMetaData MetaData, T Candidate) t) =>
                resolvedProjection(t.MetaData, t.Candidate)).ToArray() ?? Array.Empty<TResolved>(),
            value2?.Select<(TKey, ModMetaData, T), TUnresolved>(((TKey Key, ModMetaData MetaData, T Candidate) t) =>
                unresolvedProjection(t.MetaData, t.Candidate)).ToArray() ?? Array.Empty<TUnresolved>());
    }

    public static IEnumerable<(TKey Key, ModMetaData MetaData, T Candidate)> LetOuterJoin<T, TKey>(
        IEnumerable<T> candidates, Func<ModMetaData, TKey> installedKeyFactory, Func<T, TKey> candidateKeyFactory)
    {
        return from t in candidates.FullOuterJoin(ModLister.AllInstalledMods, candidateKeyFactory, installedKeyFactory,
                (c, mmd, key) => (key, mmd, c))
            where t.c != null
            select t;
    }
}