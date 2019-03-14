using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public static class OreDictionary {

        public const int MaxWeight = 1000;

        private static System.Random rand = new System.Random();

        private static SimpleCurve commonalityCurve = new SimpleCurve {
            {new CurvePoint(0.0f, 10f)},
            {new CurvePoint(0.02f, 9f)},
            {new CurvePoint(0.04f, 8f)},
            {new CurvePoint(0.06f, 6f)},
            {new CurvePoint(0.08f, 3f)},
            {new CurvePoint(float.MaxValue, 1f)}
        };

        private static Predicate<ThingDef> validOre = (
            thing => thing.mineable &&
                     thing.building != null &&
                     thing.building.isResourceRock &&
                     thing.building.mineableThing != null &&
                     thing != QuarryDefOf.MineableComponentsIndustrial
        );


        public static Dictionary<ThingDef, int> Build() {
            // Get all ThingDefs that have mineable resources
            // And assign commonality values for ores
            Dictionary<ThingDef, int> oreDictionary = DefDatabase<ThingDef>.AllDefs
                .Where(definition => validOre(definition))
                .ToDictionary(ore => ore.building.mineableThing, ValueForMineableOre);

            // Get the rarest ore in the list
            int componentsCount = oreDictionary
                .Concat(new KeyValuePair<ThingDef, int>(null, MaxWeight))
                .MinBy(pair => pair.Value)
                .Value;

            componentsCount += componentsCount / 2;

            // Manually add components
            oreDictionary.Add(ThingDefOf.ComponentIndustrial, componentsCount);
            return oreDictionary;
        }

        private static int ValueForMineableOre(ThingDef ore) {
            if (!validOre(ore)) {
                Log.Error($"Quarry:: Unable to process def {ore.defName} as a mineable resource rock.");
                return 0;
            }

            float valDeep = Mathf.Clamp(ore.building.mineableThing.deepCommonality, 0f, 1.5f);
            float valScatter = ore.building.mineableScatterCommonality *
                               commonalityCurve.Evaluate(ore.building.mineableScatterCommonality);
            float valMarket = Math.Max(ore.building.mineableThing.BaseMarketValue / 5f, 2f);

            return (int) (50f * valDeep * valScatter / valMarket);
        }

        public static float WeightAsShare(this Dictionary<ThingDef, int> ores, int weight) {
            float sum = ores.Sum(pair => pair.Value);
            return weight / sum;
        }

        public static ThingDef TakeOne() {
            
            // Make sure there is a dictionary to work from
            if (QuarrySettings.oreDictionary == null) {
                Build();
            }

            // Sorts the weight list
            var sortedWeights = QuarrySettings.oreDictionary.OrderBy(pair => pair.Value);

            // Sums all weights
            int sum = QuarrySettings.oreDictionary.Sum(pair => pair.Value);

            // Randomizes a number from Zero to Sum
            int roll = rand.Next(0, sum);
            
            // TODO: not sure it works as before
            // Finds chosen item based on weight
            ThingDef selected = sortedWeights.FirstOrDefault(pair => roll < pair.Value).Key;

            // Returns the selected item
            return selected;
        }

    }

}