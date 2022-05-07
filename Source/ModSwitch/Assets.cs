using UnityEngine;
using Verse;

namespace DoctorVanGogh.ModSwitch;

[StaticConstructorOnStartup]
public class Assets
{
    public static readonly Texture2D Edit;

    public static readonly Texture2D Delete;

    public static readonly Texture2D Settings;

    public static readonly Texture2D Document;

    public static readonly Texture2D Apply;

    public static readonly Texture2D Extract;

    public static readonly Texture2D Undo;

    public static readonly Texture2D White;

    public static readonly Texture2D SteamCopy;

    public static readonly Texture2D DragHash;

    public static readonly Texture2D WarningSmall;

    public static readonly Texture2D Debug;

    public static readonly Texture2D Collapsed;

    public static readonly Texture2D Expanded;

    static Assets()
    {
        Edit = ContentFinder<Texture2D>.Get("UI/Edit");
        Delete = ContentFinder<Texture2D>.Get("UI/Delete");
        Settings = ContentFinder<Texture2D>.Get("UI/Settings");
        Document = ContentFinder<Texture2D>.Get("UI/Document");
        Apply = ContentFinder<Texture2D>.Get("UI/Apply");
        Extract = ContentFinder<Texture2D>.Get("UI/Extract");
        Undo = ContentFinder<Texture2D>.Get("UI/Undo");
        Debug = ContentFinder<Texture2D>.Get("UI/Debug");
        White = ContentFinder<Texture2D>.Get("UI/White");
        SteamCopy = ContentFinder<Texture2D>.Get("UI/ContentSources/SteamCopy");
        DragHash = ContentFinder<Texture2D>.Get("UI/Buttons/DragHash");
        WarningSmall = ContentFinder<Texture2D>.Get("UI/Warning-small");
        Collapsed = ContentFinder<Texture2D>.Get("UI/Buttons/Dev/Reveal");
        Expanded = ContentFinder<Texture2D>.Get("UI/Buttons/Dev/Collapse");
    }
}