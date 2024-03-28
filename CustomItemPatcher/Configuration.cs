using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Patcher;

public class Configuration
{
    public const byte MinimumCustomID = 128;
    public const string ConfigFileName = "customitems.config";
    public static readonly Regex itemPattern = new(@"^\[(\d|-|!|\*)(?:,\s*(\d{1,3}))?\]\s*(\w+)\s*:\s*(\d{3})$");
    public static readonly Regex versionPattern = new(@"^@version (\d{1,3})$");

    public byte Version { get; private set; }
    public Dictionary<byte, CustomItem> CustomItems {  get; private set; }

    public static Configuration ParseText(string configString)
    {
        var lines = configString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        var versionMatch = versionPattern.Match(lines[0]);

        if (!versionMatch.Success)
        {
            throw new Exception($"Invalid format version line: \"{lines[0]}\"");
        }

        if (!byte.TryParse(versionMatch.Groups[1].Value, out var versionNumber))
        {
            throw new Exception($"Invalid format version: {versionMatch.Groups[1].Value}");
        }

        Configuration config = versionNumber switch
        {
            1 => ParseVersion1(lines),
            _ => throw new Exception($"Unsupported format version: {versionNumber}"),
        };

        return config;
    }

    private static Configuration ParseVersion1(string[] lines)
    {
        var customItems = new Dictionary<byte,CustomItem>();

        foreach (var line in lines)
        {
            // Check one: if the line is less than five characters long, ignore it
            // A valid configuration will include an asset name (1+ chars), a separator (1 char), and a three-digit ID (3 chars)
            // This allows for newlines to better space out the file
            // 
            // Check two: if the line starts with #, ignore it
            // This allows the config file to include comments
            //
            // Check three: to ignore the version metadata line
            if (line.Length < 5 || line[0] == '#' || line[0] == '@') continue;

            // Attempt to retrieve configuration details from line via pattern matching
            var match = itemPattern.Match(line);

            // If there was no match found, ignore the line
            // NOTE: Maybe an exception should be thrown?
            if (!match.Success) continue;

            // Assign capture groups to variables for easier access
            var rawLootTable = match.Groups[1].Value;
            var rawProbability = match.Groups[2].Value;
            var rawName = match.Groups[3].Value;
            var rawItemID = match.Groups[4].Value;
            
            // Retrieve type of loot table modification being made
            var modification = rawLootTable switch
            {
                "-" => LootTableModification.None,
                "!" => LootTableModification.Bosses,
                "*" => LootTableModification.All,
                _ => LootTableModification.Single,
            };

            // Instantiate loot table ID with default value
            byte lootTable = 0;

            // Attempt to parse the selected loot table ID
            // If the number chosen cannot be cast to a byte, throw an exception
            if (modification == LootTableModification.Single && !byte.TryParse(rawLootTable, out lootTable))
            {
                throw new Exception($"Custom item with name \"{rawName}\" has invalid loot table ID \"{rawLootTable}\"; please choose a valid ID from 0-2");
            }

            // If the selected loot table ID is greater than current amount of loot tables, throw an exception
            if (lootTable > 2)
            {
                throw new Exception($"Custom item with name \"{rawName}\" has invalid loot table ID \"{rawLootTable}\"; please choose a valid ID from 0-2");
            }

            // Instantiate probability with default value
            int probability = 10;

            // If a value was passed to replace the default, attempt to parse it
            // Failure to parse results in an exception
            if (rawProbability != "" && !int.TryParse(rawProbability, out probability))
            {
                throw new Exception($"Custom item with name \"{rawName}\" has invalid probability \"{rawProbability}\"; please choose a valid number between 0-999");
            }

            // Attempt to parse the chosen itemID
            // If the number chosen cannot be cast to a byte, throw an exception
            if (!byte.TryParse(rawItemID, out var itemID))
            {
                throw new Exception($"Custom item with name \"{rawName}\" has invalid ID \"{rawItemID}\"; please choose a valid number between 128-255");
            }

            // If the chosen itemID is lower than the allowed minimum, ignore it
            // There can only be 255 items in the game with its current system
            // As of 2.1.0.5, 57 slots are taken
            // I am reserving half of the possible slots for the community
            if (itemID < MinimumCustomID)
            {
                throw new Exception($"Custom item with \"{rawName}\" has invalid ID \"{rawItemID}\"; please choose a valid number between 128-255");
            }

            // If the chosen itemID overlaps an existing itemID in the mod, throw an exception
            if (customItems.ContainsKey(itemID))
            {
                throw new Exception($"Custom item with \"{rawName}\" has duplicate ID \"{rawItemID}\"; please ensure all items have unique IDs");
            }

            // Finally, if all is well, add the item to the list
            customItems.Add(itemID, new CustomItem
            {
                Name = rawName,
                ItemID = itemID,
                LootTable = lootTable,
                Probability = probability,
                Modification = modification,
            });
        }

        return new Configuration
        {
            Version = 1,
            CustomItems = customItems,
        };
    }

    public struct CustomItem
    {
        public string Name;
        public byte ItemID;
        public byte LootTable;
        public int Probability;
        public LootTableModification Modification;
    }

    public enum LootTableModification
    {
        None,
        Single,
        Bosses,
        All
    }
}
