using UnityEngine;
using Verse;
using Verse.Sound;

namespace DoctorVanGogh.ModSwitch;

[StaticConstructorOnStartup]
public static class ExtraWidgets
{
    public static readonly Texture2D ButtonBGAtlas;

    public static readonly Texture2D ButtonBGAtlasMouseover;

    public static readonly Texture2D ButtonBGAtlasClick;

    static ExtraWidgets()
    {
        ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");
        ButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");
        ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");
    }

    public static bool ButtonImage(Rect butRect, Texture2D tex, bool doMouseoverSound = false,
        TipSignal? tipSignal = null, Rect? texRect = null)
    {
        var tex2 = ButtonBGAtlas;
        if (Mouse.IsOver(butRect))
        {
            tex2 = ButtonBGAtlasMouseover;
            if (Input.GetMouseButton(0))
            {
                tex2 = ButtonBGAtlasClick;
            }
        }

        var result = Widgets.ButtonImage(butRect, tex2);
        if (doMouseoverSound)
        {
            MouseoverSounds.DoRegion(butRect);
        }

        GUI.DrawTexture(texRect ?? butRect, tex);
        if (tipSignal.HasValue)
        {
            TooltipHandler.TipRegion(butRect, tipSignal.Value);
        }

        return result;
    }
}