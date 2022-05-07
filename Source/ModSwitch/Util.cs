using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal static class Util
{
    private static readonly string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));

    private static readonly string invalidRegStr = string.Format("([{0}]*\\.+$)|([{0}]+)", invalidChars);

    private static readonly Version RW_11 = new Version(1, 1);

    private static readonly Regex rgxSteamModId = new Regex("^\\d+$", RegexOptions.Compiled | RegexOptions.Singleline);

    public static void DisplayError(Exception e, string title = null)
    {
        Warning(e.ToString());
        Find.WindowStack.Add(new Dialog_Exception(e, title));
    }

    public static void AddRange<TItem>(this ICollection<TItem> collection, IEnumerable<TItem> values)
    {
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }

    public static string Colorize(this string text, Color color)
    {
        return
            $"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}{(byte)(color.a * 255f):X2}>{text}</color>";
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        var directoryInfo = new DirectoryInfo(sourceDirName);
        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException(
                $"Source directory does not exist or could not be found: {sourceDirName}");
        }

        var directories = directoryInfo.GetDirectories();
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        var files = directoryInfo.GetFiles();
        foreach (var fileInfo in files)
        {
            var destFileName = Path.Combine(destDirName, fileInfo.Name);
            fileInfo.CopyTo(destFileName, false);
        }

        if (!copySubDirs)
        {
            return;
        }

        foreach (var directoryInfo2 in directories)
        {
            var destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
            DirectoryCopy(directoryInfo2.FullName, destDirName2, true);
        }
    }

    public static void Error(string s)
    {
        Verse.Log.Error($"[ModSwitch]: {s}");
    }

    public static void Error(Exception e)
    {
        Error(e.ToString());
    }

    public static void Log(string s)
    {
        Verse.Log.Message($"[ModSwitch]: {s}");
    }

    [Conditional("TRACE")]
    public static void Trace(string s)
    {
        Log(s);
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
    }

    public static void Warning(string s)
    {
        Verse.Log.Warning($"[ModSwitch]: {s}");
    }

    public static string SanitizeFileName(string fileName)
    {
        return Regex.Replace(fileName, invalidRegStr, "_");
    }

    public static Func<ModMetaData, string> GetVersionSpecificIdMapping(Version version)
    {
        if (version >= RW_11)
        {
            return mmd => mmd.PackageId;
        }

        return mmd => mmd.FolderName;
    }

    public static string BuildWorkshopUrl(string name, string id)
    {
        if (!rgxSteamModId.IsMatch(id))
        {
            return
                $"https://steamcommunity.com/workshop/browse/?appid=294100&searchtext={name}&browsesort=textsearch&section=items";
        }

        return $"http://steamcommunity.com/sharedfiles/filedetails/?id={id}";
    }

    public static string Combine<T>(this IEnumerable<T> items, Func<T, string> itemFormatter, string delimiter = ", ",
        string seed = "")
    {
        return items.Aggregate(new StringBuilder(seed),
            (sb, item) => sb.Length != 0
                ? sb.Append(delimiter + itemFormatter(item))
                : sb.Append(itemFormatter(item)), sb => sb.ToString());
    }

    public static string Combine(this IEnumerable<string> items, string delimiter = ", ", string seed = "")
    {
        return items.Aggregate(new StringBuilder(seed),
            (sb, item) => sb.Length != 0 ? sb.Append(delimiter + item) : sb.Append(item),
            sb => sb.ToString());
    }
}