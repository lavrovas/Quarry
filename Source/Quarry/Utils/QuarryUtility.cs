using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public static class QuarryUtility {

        public static readonly Predicate<TerrainDef> IsValidQuarryTerrain = (
            terrain => terrain == TerrainDefOf.Gravel ||
                       terrain.defName.EndsWith("_Rough") ||
                       terrain.defName.EndsWith("_RoughHewn") ||
                       terrain.defName.EndsWith("_Smooth")
        );

        public static bool IsValidQuarryRock(string terrainName) {
            if (QuarrySettings.database.NullOrEmpty()) {
                Log.Error("Quarry:: Trying to validate rock types before the database has been built.");
                return false;
            }

            // If there isn't a known chunk or blocks for this, it probably isn't a rock type and wouldn't work for spawning anyways
            // This allows Cupro's Stones to work, and any other mod that uses standard naming conventions for stones
            if (QuarrySettings.database.Find(thing => thing.defName == "Chunk" + terrainName) != null &&
                QuarrySettings.database.Find(thing => thing.defName == "Blocks" + terrainName) != null) {
                return true;
            }

            return false;
        }

        public static bool IsValidQuarryChunk(string terraingName, out ThingDef chunk) {
            chunk = null;
            if (QuarrySettings.database.NullOrEmpty()) {
                Log.Error("Quarry:: Trying to validate chunks before the database has been built.");
                return false;
            }

            chunk = QuarrySettings.database.Find(t => t.defName == "Chunk" + terraingName);
            return chunk != null;
        }

        public static bool IsValidQuarryBlocks(string terrainName, out ThingDef blocks) {
            blocks = null;
            if (QuarrySettings.database.NullOrEmpty()) {
                Log.Error("Quarry:: Trying to validate blocks before the database has been built.");
                return false;
            }

            blocks = QuarrySettings.database.Find(thing => thing.defName == "Blocks" + terrainName);
            return blocks != null;
        }

        public static IEnumerable<ThingDef> PossibleThingDefs() {
            return 
                from thing in DefDatabase<ThingDef>.AllDefs
                where (thing.category == ThingCategory.Item && thing.scatterableOnMapGen && !thing.destroyOnDrop &&
                       !thing.MadeFromStuff && thing.GetCompProperties<CompProperties_Rottable>() == null)
                select thing;
        }

    }

}