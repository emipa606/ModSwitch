using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace DoctorVanGogh.ModSwitch;

public static class Scribe_Custom
{
    public static void Look<TCollection, TItem>(ref TCollection collection, bool saveDestroyedThings, string label,
        object[] ctorArgsCollection = null, params object[] ctorArgsItem) where TCollection : ICollection<TItem>
        where TItem : IExposable
    {
        if (!Scribe.EnterNode(label))
        {
            return;
        }

        try
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (collection != null)
                {
                    foreach (var item2 in collection)
                    {
                        var target = item2;
                        Scribe_Deep.Look(ref target, saveDestroyedThings, "li", ctorArgsItem);
                    }

                    return;
                }

                Scribe.saver.WriteAttribute("IsNull", "True");
            }
            else
            {
                if (Scribe.mode != LoadSaveMode.LoadingVars)
                {
                    return;
                }

                var curXmlParent = Scribe.loader.curXmlParent;
                var xmlAttribute = curXmlParent.Attributes?["IsNull"];
                if (xmlAttribute == null || xmlAttribute.Value.ToLower() != "true")
                {
                    collection = (TCollection)Activator.CreateInstance(typeof(TCollection), ctorArgsCollection);
                    {
                        foreach (XmlNode childNode in curXmlParent.ChildNodes)
                        {
                            var item = ScribeExtractor.SaveableFromNode<TItem>(childNode, ctorArgsItem);
                            collection.Add(item);
                        }

                        return;
                    }
                }

                collection = default;
            }
        }
        finally
        {
            Scribe.ExitNode();
        }
    }
}