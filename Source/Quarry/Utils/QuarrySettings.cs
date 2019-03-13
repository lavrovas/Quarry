using System.Collections.Generic;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public class QuarrySettings : ModSettings {

        internal static bool letterSent = false;
        internal static int quarryMaxHealth = 2000;
        internal static int junkChance = 60;
        internal static int chunkChance = 50;
        internal static List<ThingCountExposable> oreDictionary = null;
        internal static List<ThingDef> database;

        internal static int QuarryMaxHealth {
            get {
                if (quarryMaxHealth > 10000) {
                    return int.MaxValue;
                }

                return quarryMaxHealth;
            }
        }


        public override void ExposeData() {
            base.ExposeData();

            Scribe_Values.Look(ref letterSent, "QRY_letterSent", false);
            Scribe_Values.Look(ref quarryMaxHealth, "QRY_quarryMaxHealth", 2000);
            Scribe_Values.Look(ref junkChance, "QRY_junkChance", 60);
            Scribe_Values.Look(ref chunkChance, "QRY_chunkChance", 50);
            Scribe_Collections.Look(ref oreDictionary, "QRY_OreDictionary");
            
            Dictionary<ThingDef, int> mineableItems = new Dictionary<ThingDef, int>();
            foreach (ThingCountExposable thingCountExposable in oreDictionary) {
                mineableItems.Add(thingCountExposable.thingDef, thingCountExposable.count);
            }
            
            Scribe_Collections.Look(ref mineableItems, "MineableItems", LookMode.Def, LookMode.Value);

            Log.Message($"Dictionary size is: {mineableItems.Count}");
            foreach (KeyValuePair<ThingDef,int> pair in mineableItems) {
                Log.Message($"Loaded {pair.Key.defName}, weight: {pair.Value}");
            }
            
            // Remove all null entries in the oreDictionary
            // This is most likely due to removing _another_ mod, which will trigger a game reset
            if (Scribe.mode != LoadSaveMode.LoadingVars)
                return;
            
           
            List<ThingCountExposable> dict = new List<ThingCountExposable>();
            bool warning = false;
            foreach (ThingCountExposable thingCount in oreDictionary) {
                if (thingCount != null) {
                    dict.Add(new ThingCountExposable(thingCount.thingDef, thingCount.count));
                }
                else if (!warning) {
                    warning = true;
                    Log.Warning(
                        "Quarry:: Found 1 or more null entries in ore dictionary. This is most likely due to an uninstalled mod. Removing entries from list.");
                }
            }

            oreDictionary = dict;
        }

    }

}