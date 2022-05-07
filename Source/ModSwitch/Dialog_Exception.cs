using System;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class Dialog_Exception : Dialog_MessageBox
{
    public Dialog_Exception(Exception e, string title = null)
        : base(e.Message, null, null, null, null, title ?? "ModSwitch.Dialog.Error".Translate())
    {
    }
}