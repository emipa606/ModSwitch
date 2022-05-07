using RimWorld;

namespace DoctorVanGogh.ModSwitch;

internal class Page_ModsConfig_Custom : Page_ModsConfig
{
    private readonly int _fixedModsHash;

    public Page_ModsConfig_Custom(int fixedModsHash)
    {
        _fixedModsHash = fixedModsHash;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        ModsConfigUI.fiPage_ModsConfig_ActiveModsWhenOpenedHash.SetValue(this, _fixedModsHash);
    }
}