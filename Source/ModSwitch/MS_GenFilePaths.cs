using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class MS_GenFilePaths
{
    public const string ExportExtension = ".rws";
    public static PropertyInfo piSavedFolderPath = AccessTools.Property(typeof(GenFilePaths), "SavedGamesFolderPath");

    public static string ModSwitchFolderPath =
        Path.Combine((string)piSavedFolderPath.GetValue(null, null), "ModSwitch");

    public static IEnumerable<FileInfo> AllExports
    {
        get
        {
            EnsureExportFolderExists();
            return from f in new DirectoryInfo(ModSwitchFolderPath).GetFiles()
                where f.Extension == ".rws"
                orderby f.LastWriteTime descending
                select f;
        }
    }

    public static string FilePathForModSetExport(string setName)
    {
        EnsureExportFolderExists();
        return Path.Combine(ModSwitchFolderPath, $"{Util.SanitizeFileName(setName)}.rws");
    }

    public static void EnsureExportFolderExists()
    {
        if (!Directory.Exists(ModSwitchFolderPath))
        {
            Directory.CreateDirectory(ModSwitchFolderPath);
        }
    }
}