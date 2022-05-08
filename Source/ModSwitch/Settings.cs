using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DoctorVanGogh.ModSwitch;

internal class Settings : ModSettings
{
    private static TipSignal? _tipCreateNew;

    private static TipSignal? _tipSettings;

    private static TipSignal? _tipApply;

    private static TipSignal? _tipUndo;

    public static FastInvokeHandler RecacheSelectedModRequirements = MethodInvoker.GetHandler(
        typeof(Page_ModsConfig).GetMethod("RecacheSelectedModRequirements",
            BindingFlags.Instance | BindingFlags.NonPublic));

    public static readonly FieldInfo fiModWarningsCached =
        AccessTools.Field(typeof(Page_ModsConfig), "modWarningsCached");

    public static AccessTools.FieldRef<Page_ModsConfig, List<string>> Page_ModsConfig_SetModWarningsCached =
        AccessTools.FieldRefAccess<Page_ModsConfig, List<string>>(fiModWarningsCached);

    public static AccessTools.FieldRef<Page_ModsConfig, List<string>> Page_ModsConfig_GetModWarningsCached =
        AccessTools.FieldRefAccess<Page_ModsConfig, List<string>>(fiModWarningsCached);

    public static object[] Empty = Array.Empty<object>();

    private Vector2 _scrollPosition;

    private ModSet _undo;

    public ModAttributesSet Attributes = new ModAttributesSet();

    public List<ModSet> Sets = new List<ModSet>();

    public static TipSignal TipSettings
    {
        get
        {
            var valueOrDefault = _tipSettings.GetValueOrDefault();
            TipSignal value;
            if (!_tipSettings.HasValue)
            {
                valueOrDefault = new TipSignal("ModSwitch.Tip.Settings".Translate());
                _tipSettings = valueOrDefault;
                value = valueOrDefault;
            }
            else
            {
                value = valueOrDefault;
            }

            return new TipSignal?(value).Value;
        }
    }

    public static TipSignal TipCreateNew
    {
        get
        {
            var valueOrDefault = _tipCreateNew.GetValueOrDefault();
            TipSignal value;
            if (!_tipCreateNew.HasValue)
            {
                valueOrDefault = new TipSignal("ModSwitch.Tip.Create".Translate());
                _tipCreateNew = valueOrDefault;
                value = valueOrDefault;
            }
            else
            {
                value = valueOrDefault;
            }

            return new TipSignal?(value).Value;
        }
    }

    public static TipSignal TipApply
    {
        get
        {
            var valueOrDefault = _tipApply.GetValueOrDefault();
            TipSignal value;
            if (!_tipApply.HasValue)
            {
                valueOrDefault = new TipSignal("ModSwitch.Tip.Apply".Translate());
                _tipApply = valueOrDefault;
                value = valueOrDefault;
            }
            else
            {
                value = valueOrDefault;
            }

            return new TipSignal?(value).Value;
        }
    }

    public static TipSignal TipUndo
    {
        get
        {
            var valueOrDefault = _tipUndo.GetValueOrDefault();
            TipSignal value;
            if (!_tipUndo.HasValue)
            {
                valueOrDefault = new TipSignal("ModSwitch.Tip.Undo".Translate());
                _tipUndo = valueOrDefault;
                value = valueOrDefault;
            }
            else
            {
                value = valueOrDefault;
            }

            return new TipSignal?(value).Value;
        }
    }

    public void DoModsConfigWindowContents(Rect target, Page_ModsConfig page)
    {
        target.x += 30f;
        var rect = new Rect(target.x, target.y, 30f, 30f);
        if (ExtraWidgets.ButtonImage(rect, Assets.Apply, false, TipApply, rect.ContractedBy(4f)) &&
            Sets.Count != 0)
        {
            Find.WindowStack.Add(new FloatMenu(Sets.Select(ms => new FloatMenuOption(ms.Name, delegate
            {
                _undo = ModSet.FromCurrent("undo", this);
                ms.Apply(page);
            })).ToList()));
        }

        var rect2 = new Rect(target.x + 30f + 8f, target.y, 30f, 30f);
        if (ExtraWidgets.ButtonImage(rect2, Assets.Extract, false, TipCreateNew,
                rect2.ContractedBy(4f)))
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("ModSwitch.CreateNew".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_SetText(delegate(string s)
                    {
                        var item = ModSet.FromCurrent(s, this);
                        Sets.Add(item);
                        Mod.WriteSettings();
                    }, "ModSwitch.Create.DefaultName".Translate()));
                }),
                new FloatMenuOption("ModSwitch.OverwritExisting".Translate(), delegate
                {
                    if (Sets.Count > 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(Sets.Select(ms => new FloatMenuOption(ms.Name,
                            delegate
                            {
                                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                {
                                    OverwriteMod(ms);
                                }
                                else
                                {
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                        "ModSwitch.OverwritExisting.Confirm".Translate(ms.Name),
                                        delegate { OverwriteMod(ms); }, true,
                                        "ModSwitch.Confirmation.Title".Translate()));
                                }
                            })).ToList()));
                    }
                })
            }));
        }

        var rect3 = new Rect(target.x + 76f, target.y, 30f, 30f);
        if (_undo != null &&
            ExtraWidgets.ButtonImage(rect3, Assets.Undo, false, TipUndo, rect3.ContractedBy(4f)))
        {
            _undo.Apply(page);
            _undo = null;
        }

        var rect4 = new Rect(320f, target.y, 30f, 30f);
        if (ExtraWidgets.ButtonImage(rect4, Assets.Settings, false, TipSettings,
                rect4.ContractedBy(4f)))
        {
            Find.WindowStack.Add(new Dialog_ModsSettings_Custom(Mod));
        }
    }

    public void DoWindowContents(Rect rect)
    {
        var listing_Standard = new Listing_Standard(GameFont.Small)
        {
            ColumnWidth = rect.width
        };
        listing_Standard.Begin(rect);
        if (Widgets.ButtonText(listing_Standard.GetRect(30f).LeftHalf(), "ModSwitch.Import".Translate(),
                true, false))
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("ModSwitch.Import.OpenFolder".Translate(), delegate
                {
                    MS_GenFilePaths.EnsureExportFolderExists();
                    Process.Start(MS_GenFilePaths.ModSwitchFolderPath);
                }),
                new FloatMenuOption("ModSwitch.Import.FromFile".Translate(), delegate
                {
                    var list = MS_GenFilePaths.AllExports.Select(fi =>
                        new FloatMenuOption(fi.Name, delegate
                        {
                            try
                            {
                                ImportFromExport(fi);
                            }
                            catch (Exception e)
                            {
                                Util.DisplayError(e);
                            }
                        })).ToList();
                    if (list.Count != 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                }),
                new FloatMenuOption("ModSwitch.Import.Savegame".Translate(),
                    delegate
                    {
                        Find.WindowStack.Add(new FloatMenu(GenFilePaths.AllSavedGameFiles.Select(fi =>
                            new FloatMenuOption(fi.Name, delegate { ImportFromSave(fi); })).ToList()));
                    }),
                new FloatMenuOption("ModListBackup", delegate
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("ModSwitch.Import.Text".Translate(),
                        "ModSwitch.Import.Choice.Replace".Translate(),
                        delegate { ImportModListBackup(true); },
                        "ModSwitch.Import.Choice.Append".Translate(), delegate { ImportModListBackup(); },
                        "ModSwitch.Confirmation.Title".Translate(), true)
                    {
                        absorbInputAroundWindow = true,
                        closeOnClickedOutside = true,
                        doCloseX = true
                    });
                })
            }));
        }

        var rect2 = listing_Standard.GetRect(rect.height - listing_Standard.CurHeight);
        var count = Sets.Count;
        Widgets.BeginScrollView(rect2, ref _scrollPosition, new Rect(0f, 0f, rect2.width - 16f, count * 34f));
        var vector = default(Vector2);
        var reorderableGroup = ReorderableWidget.NewGroup_NewTemp(delegate(int from, int to)
        {
            ReorderModSet(from, to);
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
        }, ReorderableDirection.Vertical);
        foreach (var set in Sets)
        {
            vector.y += 4f;
            var rect3 = new Rect(0f, vector.y, rect2.width - 16f, 30f);
            set.DoWindowContents(rect3, reorderableGroup);
            vector.y += 30f;
        }

        Widgets.EndScrollView();
        listing_Standard.End();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref Sets, false, "sets", LookMode.Undefined, this);
        Scribe_Custom.Look<ModAttributesSet, ModAttributes>(ref Attributes, false, "attributes",
            null, Array.Empty<object>());
    }

    private void ImportFromSave(FileInfo fi)
    {
        Scribe.loader.InitLoadingMetaHeaderOnly(fi.FullName);
        try
        {
            ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.Map,
                false);
            Scribe.loader.FinalizeLoading();
            var num = 0;
            var setname = fi.Name;
            while (Sets.Any(ms => ms.Name == setname))
            {
                setname = $"{fi.Name}_{++num}";
            }

            IEnumerable<(string, string, int)> candidates = ScribeMetaHeaderUtility.loadedModIdsList.Zip(
                ScribeMetaHeaderUtility.loadedModNamesList, (id, name) => new
                {
                    Id = id,
                    Name = name
                }).Select((t, idx) => (t.Id, t.Name, idx));
            Log.Message($"[ModSwitch]: Loaded version '{ScribeMetaHeaderUtility.loadedGameVersion}'");
            var versionSpecificIdMapping =
                Util.GetVersionSpecificIdMapping(VersionControl.VersionFromString(
                    VersionControl.VersionStringWithoutRev(ScribeMetaHeaderUtility.loadedGameVersion)));
            var tuple = ModConfigUtil.TryResolveModsList(candidates, versionSpecificIdMapping,
                ((string Id, string Name, int Index) t) => t.Id,
                (ModMetaData mmd, (string Id, string Name, int Index) t) => new
                {
                    Mod = mmd, t.Index
                }, (ModMetaData _, (string Id, string Name, int Index) t) => new { t.Name, t.Id });
            var item = tuple.Item1;
            var unresolved = tuple.Item2;
            var loadableMods = from t in item
                orderby t.Index
                select t.Mod;
            var stringBuilder =
                new StringBuilder("ModSwitch.MissingMods".Translate(Path.GetFileNameWithoutExtension(fi.Name)));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            foreach (var anon in unresolved)
            {
                stringBuilder.AppendLine($" - {anon.Name} ({anon.Id})");
            }

            if (unresolved.Any())
            {
                Find.WindowStack.Add(new Dialog_MissingMods(stringBuilder.ToString(),
                    delegate { AddModSet(setname, loadableMods); }, delegate
                    {
                        foreach (var anon2 in unresolved)
                        {
                            Process.Start(Util.BuildWorkshopUrl(anon2.Name, anon2.Id));
                        }
                    }, null));
            }

            AddModSet(setname, loadableMods);
        }
        catch (Exception ex)
        {
            Log.Warning($"Exception loading {fi.FullName}: {ex}");
            Scribe.ForceStop();
        }

        void AddModSet(string name, IEnumerable<ModMetaData> mods)
        {
            Sets.Add(new ModSet(this)
            {
                Name = name,
                BuildNumber =
                    new Version(VersionControl.VersionStringWithoutRev(ScribeMetaHeaderUtility.loadedGameVersion))
                        .Build,
                Mods = new List<string>(mods.Select(mmd => mmd.FolderName))
            });
            Mod.WriteSettings();
        }
    }

    private void ImportFromExport(FileInfo fi)
    {
        if (!File.Exists(fi.FullName))
        {
            throw new FileNotFoundException();
        }

        ModSet target = null;
        Scribe.loader.InitLoading(fi.FullName);
        try
        {
            Scribe_Deep.Look(ref target, "ModSet", this);
        }
        finally
        {
            Scribe.loader.FinalizeLoading();
        }

        if (target == null)
        {
            throw new InvalidOperationException("Error importing ModSet...");
        }

        var num = 0;
        var name = target.Name;
        while (Sets.Any(ms => ms.Name == name))
        {
            name = $"{target.Name}_{++num}";
        }

        target.Name = name;
        Sets.Add(target);
        Mod.WriteSettings();
    }

    private void ImportModListBackup(bool overwrite = false)
    {
        if (overwrite)
        {
            Attributes.Clear();
            Sets.Clear();
        }

        var text = Path.Combine(GenFilePaths.SaveDataFolderPath, "ModListBackup");
        IDictionary<int, string> dictionary = null;
        if (Directory.Exists(text))
        {
            var text2 = Path.Combine(GenFilePaths.SaveDataFolderPath, "HugsLib");
            if (Directory.Exists(text2))
            {
                var text3 = Path.Combine(text2, "ModSettings.xml");
                if (File.Exists(text3))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(text3);
                    dictionary = xmlDocument.DocumentElement?.SelectSingleNode("//ModListBackup/StateNames/text()")
                        ?.Value.Split('|').Select((v, i) => new { v, i })
                        .ToDictionary(t => t.i + 1, t => t.v);
                }
            }

            var files = Directory.GetFiles(text, "*.rws");
            if (dictionary == null)
            {
                dictionary = new Dictionary<int, string>();
            }

            var array = files;
            foreach (var text4 in array)
            {
                var xmlDocument2 = new XmlDocument();
                xmlDocument2.Load(text4);
                try
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text4);
                    string value = null;
                    if (int.TryParse(Path.GetFileNameWithoutExtension(fileNameWithoutExtension), out var result))
                    {
                        dictionary.TryGetValue(result, out value);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        value = $"MLB '{fileNameWithoutExtension}'";
                    }

                    var item = new ModSet(this)
                    {
                        Name = value,
                        Mods = (from XmlNode n in xmlDocument2.DocumentElement?.SelectNodes("//activeMods/li/text()")
                            select n.Value).ToList(),
                        BuildNumber =
                            int.Parse(
                                xmlDocument2.DocumentElement?.SelectSingleNode("//buildNumber/text()")?.Value ?? "0",
                                CultureInfo.InvariantCulture)
                    };
                    Sets.Add(item);
                }
                catch (Exception e)
                {
                    Util.Error(e);
                }
            }

            var path = Path.Combine(text, "Mod");
            if (Directory.Exists(path))
            {
                array = Directory.GetDirectories(path);
                foreach (var text5 in array)
                {
                    var text6 = Path.Combine(text5, "Settings.xml");
                    if (!File.Exists(text6))
                    {
                        continue;
                    }

                    var xmlDocument3 = new XmlDocument();
                    xmlDocument3.Load(text6);
                    var fileName = Path.GetFileName(text5);
                    if (!Attributes.TryGetValue(fileName, out var item2))
                    {
                        var attributes = Attributes;
                        var obj = new ModAttributes
                        {
                            Key = fileName
                        };
                        item2 = obj;
                        attributes.Add(obj);
                    }

                    var xmlNode = xmlDocument3.DocumentElement.SelectSingleNode("//textColor");
                    try
                    {
                        var mLBAttributes = new MLBAttributes
                        {
                            altName = xmlDocument3.DocumentElement.SelectSingleNode("//altName/text()")?.Value,
                            installName = xmlDocument3.DocumentElement.SelectSingleNode("//installName/text()")
                                ?.Value,
                            color = xmlNode != null
                                ? new Color(
                                    float.Parse(xmlNode.SelectSingleNode("r/text()")?.Value ?? "1",
                                        CultureInfo.InvariantCulture),
                                    float.Parse(xmlNode.SelectSingleNode("g/text()")?.Value ?? "1",
                                        CultureInfo.InvariantCulture),
                                    float.Parse(xmlNode.SelectSingleNode("b/text()")?.Value ?? "1",
                                        CultureInfo.InvariantCulture),
                                    float.Parse(xmlNode.SelectSingleNode("a/text()")?.Value ?? "1",
                                        CultureInfo.InvariantCulture))
                                : Color.white
                        };
                        item2.attributes.Add(mLBAttributes);
                        item2.Color = mLBAttributes.color;
                    }
                    catch (Exception e2)
                    {
                        Util.Error(e2);
                    }
                }
            }
        }

        Mod.WriteSettings();
    }

    private void OverwriteMod(ModSet ms)
    {
        var index = Sets.IndexOf(ms);
        Sets[index] = ModSet.FromCurrent(ms.Name, this);
        Mod.WriteSettings();
    }

    public void ReorderModSet(int from, int to)
    {
        if (from == to)
        {
            return;
        }

        var item = Sets[from];
        Sets.RemoveAt(from);
        Sets.Insert(to, item);
    }
}