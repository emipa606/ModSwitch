using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class ModSwitch : Mod
{
    private static readonly FieldInfo fiModLister_mods;

    public static bool IsRestartDefered;

    public static volatile IDictionary<string, uint> TSUpdateCache;

    static ModSwitch()
    {
        TSUpdateCache = new ConcurrentDictionary<string, uint>();
        fiModLister_mods = AccessTools.Field(typeof(ModLister), "mods");
    }

    public ModSwitch(ModContentPack content)
        : base(content)
    {
        var assembly = typeof(ModSwitch).Assembly;
        new Harmony("DoctorVanGogh.ModSwitch").PatchAll(assembly);
        Log.Message("[ModSwitch]: Initialized patches");
        CustomSettings = GetSettings<Settings>();
        var dictionary = Interlocked.Exchange(ref TSUpdateCache, new SteamUpdateAdapter(CustomSettings));
        if (dictionary.Count == 0)
        {
            return;
        }

        Log.Message("[ModSwitch]: Copying cached steam TS values.");
        foreach (var item in dictionary)
        {
            CustomSettings.Attributes[item.Key].LastUpdateTS = item.Value;
        }
    }

    public Settings CustomSettings { get; }

    public void DoModsConfigWindowContents(Rect bottom, Page_ModsConfig owner)
    {
        CustomSettings.DoModsConfigWindowContents(bottom, owner);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        CustomSettings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "ModSwitch".Translate();
    }

    private class SteamUpdateAdapter : IDictionary<string, uint>
    {
        private readonly Settings _owner;

        public SteamUpdateAdapter(Settings owner)
        {
            _owner = owner;
        }

        public int Count { get; }

        public bool IsReadOnly { get; }

        public uint this[string key]
        {
            get => throw new NotImplementedException();
            set => _owner.Attributes[key].LastUpdateTS = value;
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<uint> Values => throw new NotImplementedException();

        public IEnumerator<KeyValuePair<string, uint>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, uint> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, uint> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, uint>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, uint> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public void Add(string key, uint value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out uint value)
        {
            throw new NotImplementedException();
        }
    }
}