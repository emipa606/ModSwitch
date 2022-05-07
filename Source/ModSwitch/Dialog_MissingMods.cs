using System;
using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

public class Dialog_MissingMods : Window
{
    private const float TitleHeight = 42f;

    private const float ButtonHeight = 35f;

    private const float buttonCount = 3f;

    private const float buttonSpacing = 20f;

    private readonly Action _ignore;

    private readonly Action _remove;

    private readonly Action _workshop;

    public float creationRealTime;

    public Action defaultAction;

    private Vector2 scrollPosition = Vector2.zero;

    public string text;

    public Dialog_MissingMods(string text, Action ignore, Action workshop, Action remove)
    {
        this.text = text;
        defaultAction = ignore;
        _ignore = ignore;
        _workshop = workshop;
        _remove = remove;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
        creationRealTime = RealTime.LastRealTime;
        onlyOneOfTypeAllowed = false;
    }

    public ModSetAction Trigger { get; set; }

    public override Vector2 InitialSize => new Vector2(640f, 460f);

    private void AddButton(Rect inRect, int index, string label, Action action, string tooltip = null,
        bool? dangerState = null)
    {
        GUI.color = !dangerState.HasValue ? Color.white :
            dangerState.Value ? new Color(1f, 0.3f, 0.35f) : new Color(0.35f, 1f, 0.3f);
        var num = (inRect.width - 40f) / 3f;
        var rect = new Rect((index - 1) * (num + 20f), inRect.height - 35f, num, 35f);
        if (tooltip != null)
        {
            TooltipHandler.TipRegion(rect, new TipSignal(tooltip));
        }

        if (!Widgets.ButtonText(rect, label, true, false))
        {
            return;
        }

        action();
        Close();
    }

    public override void DoWindowContents(Rect inRect)
    {
        var y = inRect.y;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, y, inRect.width, 42f), "ModSwitch.MissingMods.Title".Translate());
        y += 42f;
        Text.Font = GameFont.Small;
        var outRect = new Rect(inRect.x, y, inRect.width, inRect.height - 35f - 5f - y);
        var width = outRect.width - 16f;
        var viewRect = new Rect(0f, 0f, width, Text.CalcHeight(text, width));
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), text);
        Widgets.EndScrollView();
        AddButton(inRect, 1, "ModSwitch.MissingMods.Choice.Ignore", _ignore, "ModSwitch.MissingMods.Choice.Ignore.Tip");
        AddButton(inRect, 2, "ModSwitch.MissingMods.Choice.Workshop".Translate(), _workshop,
            Trigger == ModSetAction.Apply
                ? "ModSwitch.MissingMods.Choice.Workshop_Apply.Tip".Translate()
                : "ModSwitch.MissingMods.Choice.Workshop_Import.Tip".Translate());
        if (_remove != null)
        {
            AddButton(inRect, 3, "ModSwitch.MissingMods.Choice.Remove".Translate(), _remove,
                "ModSwitch.MissingMods.Choice.Remove.Tip".Translate(), true);
        }
    }
}