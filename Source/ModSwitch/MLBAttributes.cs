using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class MLBAttributes : IExposable
{
    public string altName = string.Empty;

    public Color color = Color.white;

    public string installName = string.Empty;

    public void ExposeData()
    {
        Scribe_Values.Look(ref color, "color");
        Scribe_Values.Look(ref altName, "altName");
        Scribe_Values.Look(ref installName, "installName");
    }
}