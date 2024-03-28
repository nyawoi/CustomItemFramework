using AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Patcher;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Plugin.Patches;

[HarmonyPatch(typeof(LootController))]
public static class LootControllerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(LootController.Init))]
    public static void AddCustomItems(LootController __instance)
    {
        var tier1 = __instance.lootDistro.tier[1];
        var tier2 = __instance.lootDistro.tier[2];

        // As of 2.1.0.5, the Tier 1 and Tier 2 loot tables point to the same component
        // Changes made to one will be made to the other since they're the same
        // Therefore, we'll create a new one to separate any changes
        // This starts with a check to see if they are identical
        // If not, then there might have been a game update or a mod has overwritten Tier 2 already
        if (tier1 == tier2)
        {
            var newTier2 = __instance.gameObject.AddComponent<TierLootDistribution>();

            newTier2.equipmentRarity = tier2.equipmentRarity;
            newTier2.equipment = tier2.equipment;
            newTier2.resources = tier2.resources;

            __instance.lootDistro.tier[2] = newTier2;
        }

        // Create array to hold dynamic list of loot chances, to be able to add to them
        var amountOfTables = __instance.lootDistro.tier.Length;
        var tierEquipment = new List<TierLootDistribution.LootChance>[amountOfTables];
        var tierResources = new List<TierLootDistribution.LootChance>[amountOfTables];

        // Retrieve loot chances and store them
        for (var index = 0; index < amountOfTables; index++)
        {
            tierEquipment[index] = __instance.lootDistro.tier[index].equipment.ToList();
            tierResources[index] = __instance.lootDistro.tier[index].resources.ToList();
        }

        foreach (var config in Plugin.ItemConfiguration.Values)
        {
            if (!Enum.TryParse(config.ItemID.ToString(), out InventoryItem.ID itemID))
            {
                throw new Exception($"Failed to parse itemID \"{config.ItemID}\"; is the patcher installed properly?");
            }

            if (config.Modification == Configuration.LootTableModification.None) continue;
            
            var isResource = Plugin.ItemPrefabs[config.ItemID] is DatabaseConsumable;
            var lootChance = new TierLootDistribution.LootChance
            {
                itemID = itemID,
                probability = config.Probability,
            };
            
            if (config.Modification == Configuration.LootTableModification.Single)
            {
                if (isResource)
                {
                    tierResources[config.LootTable].Add(lootChance);
                }
                else
                {
                    tierEquipment[config.LootTable].Add(lootChance);
                }
            }
            else if (config.Modification == Configuration.LootTableModification.Bosses)
            {
                if (isResource)
                {
                    for (var index = 1; index < amountOfTables; index++)
                    {
                        tierResources[index].Add(lootChance);
                    }
                }
                else
                {
                    for (var index = 1; index < amountOfTables; index++)
                    {
                        tierEquipment[index].Add(lootChance);
                    }
                }
            }
            else if (config.Modification == Configuration.LootTableModification.All)
            {
                if (isResource)
                {
                    for (var index = 0; index < amountOfTables; index++)
                    {
                        tierResources[index].Add(lootChance);
                    }
                }
                else
                {
                    for (var index = 0; index < amountOfTables; index++)
                    {
                        tierEquipment[index].Add(lootChance);
                    }
                }
            }
        }

        // Set loot chances after additions
        for (var index = 0; index < amountOfTables; index++)
        {
            __instance.lootDistro.tier[index].equipment = tierEquipment[index].ToArray();
            __instance.lootDistro.tier[index].resources = tierResources[index].ToArray();
        }
    }
}
