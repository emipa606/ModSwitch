using System.Collections.ObjectModel;
using System.Linq;
using Verse;

namespace DoctorVanGogh.ModSwitch;

internal class ModAttributesSet : KeyedCollection<string, ModAttributes>
{
    public new ModAttributes this[string key]
    {
        get
        {
            if (TryGetValue(key, out var item))
            {
                return item;
            }

            item = new ModAttributes
            {
                Key = key
            };
            Add(item);

            return item;
        }
    }

    public ModAttributes this[ModMetaData mod] => this[mod.FolderName];

    protected override string GetKeyForItem(ModAttributes item)
    {
        return item.Key;
    }

    public bool TryGetValue(string key, out ModAttributes item)
    {
        item = Items.FirstOrDefault(ma => GetKeyForItem(ma) == key);
        return item != null;
    }
}