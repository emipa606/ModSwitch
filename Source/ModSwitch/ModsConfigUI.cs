using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ColourPicker;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace DoctorVanGogh.ModSwitch;

public static class ModsConfigUI
{
    public enum ModsChangeAction
    {
        Restart,
        Ignore,
        Query
    }

    public static readonly MethodInfo miCheckboxLabeledSelectable =
        AccessTools.Method(typeof(Widgets), "CheckboxLabeledSelectable");

    public static readonly MethodInfo miGuiSetContentColor =
        AccessTools.Property(typeof(GUI), "color").GetSetMethod(true);

    public static readonly FieldInfo fiPage_ModsConfig_ActiveModsWhenOpenedHash =
        AccessTools.Field(typeof(Page_ModsConfig), "activeModsWhenOpenedHash");

    private static readonly MethodInfo miGetModWithIdentifier =
        AccessTools.Method(typeof(ModLister), "GetModWithIdentifier");

    private static IDictionary<string, Color> _colorMap;

    public static Action restartRequiredHandler = RestartRequiredHandler;

    public static IDictionary<string, Color> ColorMap
    {
        get
        {
            object obj = _colorMap;
            if (obj != null)
            {
                return (IDictionary<string, Color>)obj;
            }

            obj = new Dictionary<string, Color>
            {
                {
                    "ModSwitch.Color.white".Translate(),
                    Color.white
                },
                {
                    "ModSwitch.Color.black".Translate(),
                    Color.black
                },
                {
                    "ModSwitch.Color.gray".Translate(),
                    Color.gray
                },
                {
                    "ModSwitch.Color.red".Translate(),
                    Color.red
                },
                {
                    "ModSwitch.Color.green".Translate(),
                    Color.green
                },
                {
                    "ModSwitch.Color.blue".Translate(),
                    Color.blue
                },
                {
                    "ModSwitch.Color.magenta".Translate(),
                    Color.magenta
                },
                {
                    "ModSwitch.Color.cyan".Translate(),
                    Color.cyan
                },
                {
                    "ModSwitch.Color.yellow".Translate(),
                    Color.yellow
                }
            };
            _colorMap = (IDictionary<string, Color>)obj;

            return (IDictionary<string, Color>)obj;
        }
    }

    public static ModsChangeAction ChangeAction { get; set; } = ModsChangeAction.Query;


    private static void CopyModLocal(ModMetaData mod, string name, StringBuilder log, bool? forceCopySettings = null,
        bool deleteExisting = false)
    {
        var text = Path.Combine(GenFilePaths.ModsFolderPath, name);
        if (deleteExisting && Directory.Exists(text))
        {
            Directory.Delete(text, true);
            Directory.CreateDirectory(text);
        }

        Util.DirectoryCopy(mod.RootDir.FullName, text, true);
        log.AppendLine("ModSwitch.CopyLocal.Result.Copy".Translate(mod.Name, text));
        log.AppendLine();
        var files = Directory.GetFiles(GenFilePaths.ConfigFolderPath);
        var pattern = $"^Mod_{mod.FolderName}_([^\\.]+).xml$";
        var rgxSettings = new Regex(pattern);
        var matching = (from s in files
            select rgxSettings.Match(Path.GetFileName(s))
            into m
            where m.Success
            select new
            {
                source = Path.Combine(GenFilePaths.ConfigFolderPath, m.Value),
                destination = Path.Combine(GenFilePaths.ConfigFolderPath,
                    ModConfigUtil.GetConfigFilename(name, m.Groups[1].Value))
            }).ToArray();
        if (forceCopySettings.HasValue)
        {
            if (forceCopySettings.GetValueOrDefault())
            {
                copySettings(true);
            }
            else
            {
                log.AppendLine("ModSwitch.CopyLocal.Result.Skipped".Translate());
            }
        }
        else if (matching.Any(t => File.Exists(t.destination)))
        {
            Find.WindowStack.Add(new Dialog_MessageBox("ModSwitch.ExistingSettings".Translate(name),
                "ModSwitch.ExistingSettings.Choice.Overwrite".Translate(), delegate { copySettings(true); },
                "ModSwitch.ExistingSettings.Choice.Skip".Translate(),
                delegate { log.AppendLine("ModSwitch.CopyLocal.Result.Skipped".Translate()); },
                "ModSwitch.Confirmation.Title".Translate(), true));
        }
        else
        {
            copySettings(false);
        }

        void copySettings(bool b)
        {
            foreach (var anon in matching)
            {
                File.Copy(anon.source, anon.destination, b);
            }

            log.AppendLine("ModSwitch.CopyLocal.Result.Settings".Translate(matching.Length));
        }
    }

    private static List<FloatMenuOption> CreateColorizationOptions(ModMetaData mod)
    {
        return ColorMap.Select(kvp => new FloatMenuOption($"{kvp.Key.Colorize(kvp.Value)} ({kvp.Key})",
                delegate { LoadedModManager.GetMod<ModSwitch>().CustomSettings.Attributes[mod].Color = kvp.Value; }))
            .ToList();
    }

    private static void DeferRestart()
    {
        Log.Message("[ModSwitch]: Defering restart!");
        ModSwitch.IsRestartDefered = true;
    }

    public static void DoContextMenu(ModMetaData mod)
    {
        var list = new List<FloatMenuOption>();
        if (mod.OnSteamWorkshop)
        {
            if (SteamAPI.IsSteamRunning())
            {
                list.Add(new FloatMenuOption("ModSwitch.CopyLocal".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_SetText(delegate(string name)
                    {
                        var log = new StringBuilder();
                        CopyModLocal(mod, name, log);
                        UpdateSteamAttributes(name, mod, log);
                        Helpers.RebuildModsList();
                        ShowLog(log, "ModSwitch.CopyLocal".Translate());
                    }, mod.Name ?? "", delegate(string name)
                    {
                        var path = Path.Combine(GenFilePaths.ModsFolderPath, name);
                        if (Path.GetInvalidPathChars().Any(name.Contains))
                        {
                            return "ModSwitch.Error.InvalidChars".Translate();
                        }

                        if (Directory.Exists(path))
                        {
                            return "ModSwitch.Error.TargetExists".Translate();
                        }

                        var directoryInfo = new DirectoryInfo(GenFilePaths.ModsFolderPath);
                        var directoryInfo2 = new DirectoryInfo(path);
                        while (directoryInfo2?.FullName != directoryInfo2?.Root.FullName)
                        {
                            if (directoryInfo2.FullName == directoryInfo.FullName)
                            {
                                return null;
                            }

                            directoryInfo2 = directoryInfo2.Parent;
                        }

                        return "ModSwitch.Error.NotValid".Translate();
                    }));
                }));
            }
            else
            {
                list.Add(new FloatMenuOption(
                    Helpers.ExplainError("ModSwitch.CopyLocal".Translate(),
                        "ModSwitch.Error.SteamNotRunning".Translate()), null));
            }
        }
        else
        {
            list.Add(new FloatMenuOption("ModSwitch.OpenFolder".Translate(),
                delegate { Process.Start(mod.RootDir.FullName); }));
            var ms = LoadedModManager.GetMod<ModSwitch>();
            var localAttributes = ms.CustomSettings.Attributes[mod];
            if (localAttributes.SteamOrigin != null)
            {
                var tsSteam = ms.CustomSettings.Attributes[localAttributes.SteamOrigin].LastUpdateTS;
                var tsCopy = localAttributes.SteamOriginTS;
                string label = "ModSwitch.Sync".Translate();
                var floatMenuOption = new FloatMenuOption(null, null);
                if (tsCopy.HasValue && tsCopy == tsSteam)
                {
                    floatMenuOption.Label = Helpers.ExplainError(label, "ModSwitch.Sync.Identical".Translate());
                }
                else
                {
                    floatMenuOption.Label = label;
                    floatMenuOption.action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "ModSwitch.Sync.Message".Translate(mod.Name, Helpers.WrapTimestamp(tsCopy),
                                Helpers.WrapTimestamp(tsSteam)), "ModSwitch.Sync.Choice.KeepSettings".Translate(),
                            delegate { SyncSteam(mod, localAttributes.SteamOrigin, false); },
                            "ModSwitch.Sync.Choice.CopySteam".Translate(),
                            delegate { SyncSteam(mod, localAttributes.SteamOrigin, true); },
                            "ModSwitch.Confirmation.Title".Translate())
                        {
                            doCloseX = true,
                            closeOnClickedOutside = true
                        });
                    };
                }

                list.Add(floatMenuOption);
            }
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (SteamAPI.IsSteamRunning())
                {
                    var installed = ModLister.AllInstalledMods.ToArray();
                    if (installed.Length != 0)
                    {
                        list.Add(new FloatMenuOption("ModSwitch.SetOrigin".Translate(), delegate
                        {
                            Find.WindowStack.Add(new FloatMenu((from mmd in installed
                                where mmd.OnSteamWorkshop
                                select new FloatMenuOption(mmd.Name, delegate
                                {
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                        "ModSwitch.SetOrigin.Confirm".Translate(mod.Name, mmd.Name), delegate
                                        {
                                            ms.CustomSettings.Attributes[mod.FolderName].SteamOrigin = mmd.FolderName;
                                            Helpers.RebuildModsList();
                                        }, true, "ModSwitch.Confirmation.Title".Translate()));
                                })).ToList()));
                        }));
                    }
                }
                else
                {
                    list.Add(new FloatMenuOption(
                        Helpers.ExplainError("ModSwitch.SetOrigin".Translate(),
                            "ModSwitch.Error.SteamNotRunning".Translate()), null));
                }
            }
        }

        var currentColour = Color.white;
        var color = LoadedModManager.GetMod<ModSwitch>().CustomSettings.Attributes[mod].Color;
        if (color != null)
        {
            currentColour = (Color)color;
        }

        list.Add(new FloatMenuOption("ModSwitch.Color".Translate(),
            delegate
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(currentColour,
                    newColour =>
                    {
                        LoadedModManager.GetMod<ModSwitch>().CustomSettings.Attributes[mod].Color = newColour;
                    }));
                //Find.WindowStack.Add(new FloatMenu(CreateColorizationOptions(mod)));
            }));

        Find.WindowStack.Add(new FloatMenu(list));
    }

    public static void DrawContentSource(Rect r, ContentSource source, Action clickAction, ModMetaData mod)
    {
        if (string.IsNullOrEmpty(LoadedModManager.GetMod<ModSwitch>().CustomSettings.Attributes[mod].SteamOrigin))
        {
            ContentSourceUtility.DrawContentSource(r, source,
                source == ContentSource.ModsFolder
                    ? clickAction ?? delegate { Process.Start(mod.RootDir.FullName); }
                    : clickAction);
        }
        else
        {
            var rect = new Rect(r.x, r.y + (r.height / 2f) - 12f, 24f, 24f);
            GUI.DrawTexture(rect, Assets.SteamCopy);
            Widgets.DrawHighlightIfMouseover(rect);
            TooltipHandler.TipRegion(rect, () => "Source".Translate() + ": " + "ModSwitch.Source.SteamCopy".Translate(),
                (int)(r.x + (r.y * 56161f)));
            if (Widgets.ButtonInvisible(rect, false))
            {
                Process.Start(mod.RootDir.FullName);
            }
        }

        if (!mod.VersionCompatible)
        {
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + (r.height / 2f) - 12f + 4f, 20f, 20f), Assets.WarningSmall);
        }
    }

    public static void OnModsChanged()
    {
        switch (ChangeAction)
        {
            case ModsChangeAction.Restart:
                Find.WindowStack.Add(new Dialog_MessageBox("ModsChanged".Translate(), null, GenCommandLine.Restart));
                break;
            case ModsChangeAction.Query:
                if (!ModSwitch.IsRestartDefered)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("ModsChanged".Translate(),
                        "ModSwitch.RestartRequired.Restart".Translate(), GenCommandLine.Restart,
                        "ModSwitch.RestartRequired.Defer".Translate(), DeferRestart, null, true));
                }

                break;
            case ModsChangeAction.Ignore:
                break;
        }
    }

    public static ListableOption WrapMainMenuOption(string label, Action action, string uiHighlightTag = null)
    {
        return new ListableOption(label, delegate
        {
            if (ModSwitch.IsRestartDefered)
            {
                RestartRequiredHandler();
            }
            else
            {
                action();
            }
        }, uiHighlightTag);
    }

    private static void RestartRequiredHandler()
    {
        Find.WindowStack.Add(new Dialog_MessageBox("ModsChanged".Translate(), null, GenCommandLine.Restart)
        {
            doCloseX = true
        });
    }

    private static void ShowLog(StringBuilder log, string title)
    {
        Find.WindowStack.Add(new Dialog_MessageBox(log.ToString())
        {
            title = title
        });
    }

    private static void SyncSteam(ModMetaData mod, string steamId, bool forceCopySettings)
    {
        var log = new StringBuilder();
        var metadata = Helpers.GetMetadata(steamId);
        CopyModLocal(metadata, mod.FolderName, log, forceCopySettings, true);
        UpdateSteamAttributes(mod.FolderName, metadata, log);
        Helpers.RebuildModsList();
        ShowLog(log, "ModSwitch.Sync".Translate());
    }

    private static void UpdateSteamAttributes(string name, ModMetaData original, StringBuilder log)
    {
        if (!original.OnSteamWorkshop)
        {
            throw new ArgumentException();
        }

        var mod = LoadedModManager.GetMod<ModSwitch>();
        var modAttributes = mod.CustomSettings.Attributes[name];
        modAttributes.SteamOrigin = original.FolderName;
        modAttributes.SteamOriginTS = mod.CustomSettings.Attributes[original].LastUpdateTS;
        if (!modAttributes.SteamOriginTS.HasValue)
        {
            log.AppendLine();
            log.AppendLine("ModSwitch.CopyLocal.Result.TimestampUnknown".Translate());
        }

        mod.WriteSettings();
    }

    public static class Helpers
    {
        public static string ExplainError(string label, string error)
        {
            return $"{label} *{error}*";
        }

        public static void ForceSteamWorkshopRequery()
        {
            AccessTools.Method(typeof(WorkshopItems), "RebuildItemsList").Invoke(null, null);
        }

        public static ModMetaData GetMetadata(string identifier)
        {
            var miGetModWithIdentifier = ModsConfigUI.miGetModWithIdentifier;
            object[] parameters = { identifier };
            return (ModMetaData)miGetModWithIdentifier.Invoke(null, parameters);
        }

        public static void RebuildModsList()
        {
            AccessTools.Method(typeof(ModLister), "RebuildModList").Invoke(null, null);
        }

        public static Color SetGUIColorMod(ModMetaData mod)
        {
            var contentColor = GUI.contentColor;
            GUI.color = LoadedModManager.GetMod<ModSwitch>().CustomSettings.Attributes[mod].Color ?? Color.white;
            return contentColor;
        }

        public static void UpdateSteamTS(PublishedFileId_t pfid, uint ts)
        {
            ModSwitch.TSUpdateCache[pfid.ToString()] = ts;
        }

        public static string WrapTimestamp(long? timestamp)
        {
            if (!timestamp.HasValue)
            {
                return $"<i>{"ModSwitch.Sync.UnknownTimestamp".Translate()}</i>";
            }

            return Util.UnixTimeStampToDateTime(timestamp.Value).ToString("g");
        }
    }

    public static class Search
    {
        public const float buttonSize = 24f;

        public const float buttonsInset = 2f;

        private const float SearchDefaultHeight = 29f;

        private const float SearchClearDefaultSize = 12f;

        private const string searchControlName = "msSearch";

        public static string searchTerm;

        public static bool searchFocused;

        public static readonly GUIStyle DefaultSearchBoxStyle;

        static Search()
        {
            searchTerm = string.Empty;
            Text.Font = GameFont.Small;
            DefaultSearchBoxStyle = new GUIStyle(Text.CurTextFieldStyle);
        }

        public static void DoSearchBlock(Rect area, string weatermark, GUIStyle style = null)
        {
            var val = area.height / 29f;
            var num = 12f * Math.Min(1f, val);
            var num2 = Widgets.ButtonImage(
                new Rect(area.xMax - 4f - num, area.y + ((area.height - num) / 2f), num, num), Widgets.CheckboxOffTex);
            var text = searchTerm != string.Empty || searchFocused ? searchTerm : weatermark;
            if (!searchFocused)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
            }

            GUI.SetNextControlName("msSearch");
            var text2 = GUI.TextField(area, text, style ?? DefaultSearchBoxStyle);
            GUI.color = Color.white;
            if (searchFocused)
            {
                searchTerm = text2;
            }

            if ((GUI.GetNameOfFocusedControl() == "msSearch" || searchFocused) &&
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape ||
                 !Mouse.IsOver(area) && Event.current.type == EventType.MouseDown))
            {
                GUIUtility.keyboardControl = 0;
                searchFocused = false;
            }
            else if (GUI.GetNameOfFocusedControl() == "msSearch" && !searchFocused)
            {
                searchFocused = true;
            }

            if (num2)
            {
                searchTerm = string.Empty;
            }
        }

        public static bool MatchCriteria(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase) != -1;
            }

            return true;
        }
    }
}