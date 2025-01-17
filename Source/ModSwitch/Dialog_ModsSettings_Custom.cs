using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class Dialog_ModsSettings_Custom : Dialog_ModSettings
{
    private readonly Mod _mod;

    private int _initialHash;

    public Dialog_ModsSettings_Custom(Mod mod)
    {
        _mod = mod;
    }

    public override void PreClose()
    {
        base.PreClose();
        ModsConfigUI.ChangeAction = ModsConfigUI.ModsChangeAction.Query;
        Find.WindowStack.Add(new Page_ModsConfig_Custom(_initialHash));
    }

    public override void PreOpen()
    {
        var page_ModsConfig = Find.WindowStack.Windows.OfType<Page_ModsConfig>().FirstOrDefault();
        _initialHash = (int)ModsConfigUI.fiPage_ModsConfig_ActiveModsWhenOpenedHash.GetValue(page_ModsConfig);
        ModsConfigUI.ChangeAction = ModsConfigUI.ModsChangeAction.Ignore;
        page_ModsConfig?.Close();
        base.PreOpen();
        AccessTools.Field(typeof(Dialog_ModSettings), "selMod").SetValue(this, _mod);
    }
}