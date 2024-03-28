using HarmonyLib;
using System;

namespace AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Plugin.Patches;

[HarmonyPatch(typeof(ItemsBase))]
public static class ItemsBasePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemsBase.Init))]
    public static void AddCustomItems(ItemsBase __instance)
    {
        for (var i = __instance.item.Count; i < byte.MaxValue; i++)
        {
            __instance.item.Add(__instance.item[0]);
        }

        foreach (var kvPair in Plugin.ItemPrefabs)
        {
            if (!Enum.TryParse(kvPair.Key.ToString(), out InventoryItem.ID itemID))
            {
                throw new Exception($"Failed to parse itemID \"{kvPair.Key}\"; is the patcher installed properly?");
            }

            kvPair.Value.itemID = itemID;

            switch (kvPair.Value)
            {
                case DatabaseConsumable dbConsumable:
                    dbConsumable.consumablePrefab.GetComponent<EquippedProp>().itemID = itemID;
                    break;
                case DatabaseGun dbGun:
                    dbGun.gunPrefab.GetComponent<EquippedProp>().itemID = itemID;
                    break;
                case DatabaseMelee dbMelee:
                    dbMelee.meleePrefab.GetComponent<EquippedProp>().itemID = itemID;
                    break;
                case DatabaseThrowable dbThrowable:
                    dbThrowable.handPrefab.GetComponent<EquippedProp>().itemID = itemID;
                    break;
            }

            __instance.item[kvPair.Key] = kvPair.Value;
        }
    }
}
