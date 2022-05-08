using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;
#pragma warning disable CS0252, CS0253

namespace DoctorVanGogh.ModSwitch;

internal class Patches
{
    public class ModsConfig_DoWindowContents
    {
        [HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
        public class InjectSearchBox
        {
            public static Rect AllocateAndDrawSearchboxRect(Rect r)
            {
                ModsConfigUI.Search.DoSearchBlock(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, 24f),
                    "ModSwitch.Search.Watermark".Translate());
                return new Rect(r.x, r.y + 28f, r.width, r.height - 28f);
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
            {
                var list = new List<CodeInstruction>(instr);
                var num = list.FirstIndexOf(ci =>
                    ci.opcode == OpCodes.Call && ci.operand == AccessTools.Method(typeof(Widgets), "BeginScrollView"));
                if (num == -1)
                {
                    Util.Error(
                        "Could not find Page_ModsConfig.DoWindowContents transpiler anchor - not injecting code");
                    return list;
                }

                list.Insert(num - 4,
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(InjectSearchBox), nameof(AllocateAndDrawSearchboxRect))));
                return list;
            }
        }

        [HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
        public class DrawOperationButtons
        {
            public static void Postfix(Page_ModsConfig __instance, Rect rect)
            {
                LoadedModManager.GetMod<ModSwitch>()
                    ?.DoModsConfigWindowContents(new Rect(0f, rect.height - 52f + 8f, 350f, 44f), __instance);
            }
        }
    }

    [HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.PreOpen))]
    public class Page_ModsConfig_PreOpen
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var list = new List<CodeInstruction>(instr);
            var miTarget = AccessTools.Method(typeof(ModLister), nameof(ModLister.RebuildModList));
            var num = list.FirstIndexOf(ci => ci.opcode == OpCodes.Call && ci.operand == miTarget);
            if (num == -1)
            {
                Util.Warning("Could not find Page_ModsConfig.PreOpen transpiler anchor - not injecting code.");
                return list;
            }

            list[num].operand = AccessTools.Method(typeof(ModsConfigUI.Helpers),
                nameof(ModsConfigUI.Helpers.ForceSteamWorkshopRequery));
            return list;
        }
    }

    public class Page_ModsConfig_DoModRow
    {
        [HarmonyPatch(typeof(Page_ModsConfig), "DoModRow")]
        public class SupressNonMatchingFilteredRows
        {
            public static bool Prefix(ModMetaData mod)
            {
                if (!ModsConfigUI.Search.MatchCriteria(mod?.Name))
                {
                    return true == mod?.SupportedVersionsReadOnly.Any(version =>
                        ModsConfigUI.Search.MatchCriteria(version.ToString()));
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Page_ModsConfig), "DoModRow")]
        public class InjectRightClickMenu
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator ilGen)
            {
                var list = new List<CodeInstruction>(instructions);
                var num = list.FirstIndexOf(ci =>
                    ci.opcode == OpCodes.Call && ci.operand == ModsConfigUI.miCheckboxLabeledSelectable);
                if (num == -1)
                {
                    Util.Warning("Could not find anchor for ModRow transpiler - not modifying code");
                    return list;
                }

                var lblBlockEnd = (Label)list[num + 1].operand;
                var num2 = list.FindIndex(num + 1, ci => ci.labels.Contains(lblBlockEnd));
                if (num2 == -1)
                {
                    Util.Warning("Could not find end Label for ModRow transpiler change - not modifying code");
                    return list;
                }

                var operand = ilGen.DeclareLocal(typeof(Color));
                var label = ilGen.DefineLabel();
                var label2 = ilGen.DefineLabel();
                list.InsertRange(num2, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc, operand)
                    {
                        labels = new List<Label> { label2 }
                    },
                    new CodeInstruction(OpCodes.Call, ModsConfigUI.miGuiSetContentColor)
                });
                list[num + 2].labels.Add(label);
                list.InsertRange(num + 2, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc, operand),
                    new CodeInstruction(OpCodes.Call, ModsConfigUI.miGuiSetContentColor),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonUp))),
                    new CodeInstruction(OpCodes.Brfalse, label),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ModsConfigUI), nameof(ModsConfigUI.DoContextMenu))),
                    new CodeInstruction(OpCodes.Br, lblBlockEnd)
                });
                list[num + 1].operand = label2;
                list.InsertRange(num - 4, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ModsConfigUI.Helpers), nameof(ModsConfigUI.Helpers.SetGUIColorMod))),
                    new CodeInstruction(OpCodes.Stloc, operand)
                });
                return list;
            }
        }

        [HarmonyPatch(typeof(Page_ModsConfig), "DoModRow")]
        public class InjectCustomContentSourceDraw
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
            {
                var list = new List<CodeInstruction>(instr);
                var miTarget = AccessTools.Method(typeof(ContentSourceUtility),
                    nameof(ContentSourceUtility.DrawContentSource));
                var num = list.FirstIndexOf(ci => ci.opcode == OpCodes.Call && ci.operand == miTarget);
                if (num == -1)
                {
                    Util.Error("Could not find DrawContentSource transpiler anchor - not injecting code.");
                    return list;
                }

                list[num].operand = AccessTools.Method(typeof(ModsConfigUI), nameof(ModsConfigUI.DrawContentSource));
                list.Insert(num, new CodeInstruction(OpCodes.Ldarg_2));
                return list;
            }
        }
    }

    [HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.PostClose))]
    public class Page_ModsConfig_PostClose
    {
        private static readonly MethodInfo mi = typeof(ModsConfig).GetMethod(nameof(ModsConfig.RestartFromChangedMods));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand == mi)
                {
                    instruction.operand = typeof(ModsConfigUI).GetMethod(nameof(ModsConfigUI.OnModsChanged));
                }

                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.SelectMod))]
    public class Page_ModsConfig_SelectMod
    {
        public static void Postfix(ref Vector2 ___modListScrollPosition, List<ModMetaData> ___modsInListOrderCached,
            ModMetaData mod)
        {
            var modOrder = ___modsInListOrderCached.IndexOf(mod);
            if (modOrder == -1)
            {
                return;
            }

            ___modListScrollPosition.y = modOrder * 26f;
        }
    }

    [HarmonyPatch(typeof(ModsConfig), nameof(ModsConfig.Save))]
    public class ModsConfig_Save
    {
        public static void Postfix()
        {
            LoadedModManager.GetMod<ModSwitch>()?.WriteSettings();
        }
    }

    [HarmonyPatch(typeof(WorkshopItem), nameof(WorkshopItem.MakeFrom))]
    public class WorkshopItem_MakeFrom
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var list = new List<CodeInstruction>(instr);
            var ciTarget = AccessTools.Constructor(typeof(WorkshopItem_Mod));
            var miAnchor = AccessTools.DeclaredMethod(typeof(SteamUGC), nameof(SteamUGC.GetItemInstallInfo));
            var num = list.FirstIndexOf(ci => ci.opcode == OpCodes.Newobj && ci.operand == ciTarget);
            if (-1 == num)
            {
                Util.Warning("Could not find WorkshopItem.MakeFrom transpiler anchor - not injecting code");
                return list;
            }

            var num2 = list.FirstIndexOf(ci => ci.opcode == OpCodes.Call && ci.operand == miAnchor);
            if (num2 == -1)
            {
                Util.Warning("Could not find SteamUGC.GetItemInstallInfo transpiler anchor - not injecting code");
                return list;
            }

            var opcode = list[num2 - 1].opcode;
            if (opcode == OpCodes.Ldloca || opcode == OpCodes.Ldloca_S)
            {
                var localBuilder = (LocalBuilder)list[num2 - 1].operand;
                list.InsertRange(num, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc, localBuilder),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ModsConfigUI.Helpers), nameof(ModsConfigUI.Helpers.UpdateSteamTS)))
                });
                return list;
            }

            Util.Warning("Could not find SteamUGC.GetItemInstallInfo TS local - not injecting code");
            return list;
        }
    }

    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    public class MainMenuDrawer_DoMainMenuControls
    {
        private static readonly ConstructorInfo ciNewListableOption = AccessTools.Constructor(typeof(ListableOption),
            new[]
            {
                typeof(string),
                typeof(Action),
                typeof(string)
            });

        private static readonly MethodInfo miWrappedMenuOption =
            AccessTools.Method(typeof(ModsConfigUI), nameof(ModsConfigUI.WrapMainMenuOption));

        private static bool InjectDeferedRestartHint(List<CodeInstruction> instructions, ILGenerator ilGen,
            string anchor)
        {
            if (ilGen == null)
            {
                throw new ArgumentNullException(nameof(ilGen));
            }

            var num = instructions.FirstIndexOf(ci => ci.opcode == OpCodes.Ldstr && ci.operand as string == anchor);
            if (num == -1)
            {
                Util.Warning($"Could not find DoMainMenuControls {anchor} anchor - not injecting code");
                return false;
            }

            var num2 = instructions.FindIndex(num,
                ci => ci.opcode == OpCodes.Newobj && ci.operand == ciNewListableOption);
            if (num2 == -1)
            {
                Util.Warning($"Could not find DoMainMenuControls {anchor} ListOption constructor - not injecting code");
                return false;
            }

            instructions[num2].opcode = OpCodes.Call;
            instructions[num2].operand = miWrappedMenuOption;
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator ilGen)
        {
            var list = new List<CodeInstruction>(instr);
            if (!InjectDeferedRestartHint(list, ilGen, "Tutorial"))
            {
                return list;
            }

            if (!InjectDeferedRestartHint(list, ilGen, "NewColony"))
            {
                return list;
            }

            InjectDeferedRestartHint(list, ilGen, "LoadGame");
            return list;
        }
    }
}