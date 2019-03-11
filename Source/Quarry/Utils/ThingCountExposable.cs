﻿using System;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public sealed class ThingCountExposable : IExposable, IComparable<ThingCountExposable> {

        public ThingDef thingDef;
        public int count;

        public ThingCountExposable(ThingDef thingDef, int count) {
            this.thingDef = thingDef;
            this.count = count;
        }

        // TODO: consider if its nessesary
        public override string ToString() {
            return $"({count}x {thingDef?.defName ?? "null"})";
        }

        // TODO: consider if its nessesary
        public int CompareTo(ThingCountExposable other) {
            return count.CompareTo(other.count);
        }

        // TODO: not sure how this works
        public void ExposeData() {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref count, "count", 0, false);

            if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs || thingDef != null)
                return;
            
            Log.Warning("Quarry:: Failed to load ThingCount. Setting default.");
            thingDef = ThingDefOf.Steel;
            count = (count <= 0) ? 10 : count;
        }

    }

}