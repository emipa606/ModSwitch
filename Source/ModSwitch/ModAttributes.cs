using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class ModAttributes : IExposable
{
    public List<IExposable> attributes = new List<IExposable>();

    public Color? Color;

    public string Key = string.Empty;

    public long? LastUpdateTS;

    public string SteamOrigin;

    public long? SteamOriginTS;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Key, "key");
        Scribe_Collections.Look(ref attributes, false, "attributes");
        Scribe_Values.Look(ref Color, "color");
        Scribe_Values.Look(ref SteamOrigin, "origin");
        Scribe_Values.Look(ref SteamOriginTS, "originTS");
        if (Scribe.mode == LoadSaveMode.LoadingVars && !Color.HasValue)
        {
            Color = attributes.OfType<MLBAttributes>().FirstOrDefault()?.color;
        }
    }
}