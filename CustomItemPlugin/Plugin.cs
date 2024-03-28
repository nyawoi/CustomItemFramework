using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Patcher;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Plugin;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGUID = "AetharNet.Mods.ZumbiBlocks2.CustomItemFramework.Plugin";
    public const string PluginAuthor = "awoi";
    public const string PluginName = "CustomItemPlugin";
    public const string PluginVersion = "0.1.0";

    public static readonly Dictionary<byte, Configuration.CustomItem> ItemConfiguration = new();
    public static readonly Dictionary<byte, DatabaseItem> ItemPrefabs = new();

    internal new static ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var pluginsDirectory = Directory.GetParent(assemblyDirectory);

        foreach (var pluginDirectory in pluginsDirectory.GetDirectories())
        {
            var configFilePath = Path.Combine(pluginDirectory.FullName, Configuration.ConfigFileName);
            var assetBundlePath = Path.Combine(pluginDirectory.FullName, "customitems.bundle");
            var hasConfig = File.Exists(configFilePath);
            var hasBundle = File.Exists(assetBundlePath);

            // If neither config nor bundle are present, this mod is not using CIF
            if (!hasConfig && !hasBundle) continue;

            // These checks are for debugging purposes
            // A new developer may have one file but lacks the other
            // This will result in a warning so the developer knows what to fix
            if (hasConfig && !hasBundle)
            {
                Logger.LogWarning($"Configuration file found with no bundle: {configFilePath}");
                continue;
            }
            if (!hasConfig && hasBundle)
            {
                Logger.LogWarning($"Bundle found with no configuration file: {assetBundlePath}");
                continue;
            }

            var configText = File.ReadAllText(configFilePath);
            var configData = Configuration.ParseText(configText);
            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);

            var customItemsGO = assetBundle.LoadAsset<GameObject>("Assets/Custom/CustomItems.prefab");
            var dbItemNameMap = new Dictionary<string, DatabaseItem>();

            foreach (var dbItem in customItemsGO.GetComponents<DatabaseItem>())
            {
                dbItemNameMap.Add(dbItem.nameTag, dbItem);
            }

            foreach (var kvPair in configData.CustomItems)
            {
                // If the chosen itemID has already been taken by another mod, throw an exception
                // Mod developers are reccommended to communicate with each other as to which IDs they are using
                // This prevents conflicts between two or more mods attempting to use the same ID
                if (ItemConfiguration.ContainsKey(kvPair.Key))
                {
                    throw new Exception($"Custom item with name of \"{kvPair.Value.Name}\" has a conflicting ID of {kvPair.Key}; please refer to the #modding channel to discuss ID usage");
                }

                if (!dbItemNameMap.ContainsKey(kvPair.Value.Name))
                {
                    Logger.LogWarning($"Configuration found for an item that was not included in bundle ({kvPair.Value.Name}); check your item's nameTag");
                    continue;
                }

                ItemConfiguration.Add(kvPair.Key, kvPair.Value);
                ItemPrefabs.Add(kvPair.Key, dbItemNameMap[kvPair.Value.Name]);
            }
        }

        Logger.LogInfo($"Successfully imported {ItemConfiguration.Count} custom items");

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }
}
