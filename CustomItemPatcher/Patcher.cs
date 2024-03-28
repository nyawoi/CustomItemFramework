using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Patcher;

public static class Patcher
{
    private const FieldAttributes EnumFieldAttributes = FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault;

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly)
    {
        var assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var pluginsDirectoryPath = Path.Combine(assemblyDirectoryPath, "../../plugins");
        var customItemsMap = new Dictionary<byte, string>();

        foreach (var pluginDirectoryPath in Directory.GetDirectories(pluginsDirectoryPath))
        {
            var configFilePath = Path.Combine(pluginDirectoryPath, Configuration.ConfigFileName);

            if (!File.Exists(configFilePath)) continue;

            var configText = File.ReadAllText(configFilePath);
            var configData = Configuration.ParseText(configText);

            foreach (var item in configData.CustomItems.Values)
            {
                if (customItemsMap.ContainsKey(item.ItemID))
                {
                    throw new Exception($"Custom item with name of \"{item.Name}\" has a conflicting ID of {item.ItemID}; please refer to the #modding channel to discuss ID usage");
                }

                customItemsMap.Add(item.ItemID, item.Name);
            }
        }

        var InventoryItem = assembly.MainModule.Types.First(type => type.Name == "InventoryItem");
        var ID = InventoryItem.NestedTypes.First(type => type.Name == "ID");

        foreach (var kvPair in customItemsMap)
        {
            var newItemID = new FieldDefinition(kvPair.Value, EnumFieldAttributes, ID)
            {
                Constant = kvPair.Key
            };

            ID.Fields.Add(newItemID);
        }
    }
}
