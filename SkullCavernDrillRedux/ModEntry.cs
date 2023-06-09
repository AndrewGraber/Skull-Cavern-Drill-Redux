﻿using HarmonyLib;
using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SkullCavernDrillRedux
{
    public interface IApi
    {
        int GetBigCraftableId(string name);
    }
    
    internal sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            ObjectPatches.ApplyPatch(this.ModManifest.UniqueID);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ObjectPatches.Initialize(Helper, Monitor);
        }

        internal class ObjectPatches
        {
            private static IApi api;
            private static IMonitor Monitor;
            private static int MyCraftableID = -1;

            //Create the patch inside this class, rather than in ModEntry
            public static void ApplyPatch(string modId)
            {
                var harmony = new Harmony(modId);

                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction)),
                    prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.PlacementAction_Prefix))
                );
            }

            //Get JsonAssets API and the Drill's item ID
            public static void Initialize(IModHelper helper, IMonitor monitor)
            {
                api = helper.ModRegistry.GetApi<IApi>("spacechase0.JsonAssets");
                Monitor = monitor;

                if(api != null)
                {
                    MyCraftableID = api.GetBigCraftableId("Dwarven Drill");
                }
            }

            internal static bool PlacementAction_Prefix(StardewValley.Object __instance, GameLocation location, int x, int y, ref bool __result, Farmer who = null)
            {
                try
                {
                    //Checks if you're holding the drill, and if you're in the Skull Cavern
                    if((__instance.bigCraftable.Value && __instance.ParentSheetIndex == MyCraftableID) && (location is MineShaft shaft && Game1.CurrentMineLevel > 120))
                    {
                        //Convert from pixel coordinates to tile coordinates
                        shaft.createLadderDown((x / 64), (y / 64), true);
                        __result = true;
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed in {nameof(PlacementAction_Prefix)}:\n{ex}", LogLevel.Error);
                    return true;
                }
            }
        }
    }
}
