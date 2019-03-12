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


        public static void Build() {
            // Get all ThingDefs that have mineable resources
            IEnumerable<ThingDef> ores = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => validOre(def));

            // Assign commonality values for ores
            List<ThingCountExposable> oreDictionary = 
                ores.Select(ore => new ThingCountExposable(ore.building.mineableThing, ValueForMineableOre(ore)))
                    .ToList();

            // Get the rarest ore in the list
            int componentsCount = oreDictionary
                .Select(thingCount => thingCount.count)
                .Concat(new[] {MaxWeight})
                .Min();

            componentsCount += componentsCount / 2;

            // Manually add components
            oreDictionary.Add(new ThingCountExposable(ThingDefOf.ComponentIndustrial, componentsCount));

            // Assign this dictionary for the mod to use
            QuarrySettings.oreDictionary = oreDictionary;
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


        public static float WeightAsPercentageOf(this IEnumerable<ThingCountExposable> dictionary, int weight) {
            float sum = dictionary.Sum(thingCount => thingCount.count);

            return (weight / sum) * 100f;
        }


        public static ThingDef TakeOne() {
            
            // Make sure there is a dictionary to work from
            if (QuarrySettings.oreDictionary == null) {
                Build();
            }

            // Sorts the weight list
            List<ThingCountExposable> sortedWeights = Sort(QuarrySettings.oreDictionary);

            // Sums all weights
            int sum = QuarrySettings.oreDictionary.Sum(t => t.count);

            // Randomizes a number from Zero to Sum
            int roll = rand.Next(0, sum);

            // Finds chosen item based on weight
            ThingDef selected = sortedWeights[sortedWeights.Count - 1].thingDef;
            for (int j = 0; j < sortedWeights.Count; j++) {
                if (roll < sortedWeights[j].count) {
                    selected = sortedWeights[j].thingDef;
                    break;
                }

                roll -= sortedWeights[j].count;
            }

            // Returns the selected item
            return selected;
        }


        private static List<ThingCountExposable> Sort(List<ThingCountExposable> weights) {
            List<ThingCountExposable> list = new List<ThingCountExposable>(weights);

            // Sorts the Weights List for randomization later
            list.Sort();

            return list;
        }

    }

}