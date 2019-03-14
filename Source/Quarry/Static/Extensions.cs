using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public static class Extensions {

        public static int RoundToAsInt(this float num, int factor) {
            return (int) (Math.Round(num / (double) factor, 0) * factor);
        }


        public static Rect LeftThird(this Rect rect) {
            return new Rect(rect.x, rect.y, rect.width / 3f, rect.height);
        }


        public static Rect MiddleThird(this Rect rect) {
            return new Rect(rect.x + rect.width / 3f, rect.y, rect.width / 3f, rect.height);
        }


        public static Rect RightThird(this Rect rect) {
            return new Rect(rect.x + rect.width / 1.5f, rect.y, rect.width / 3f, rect.height);
        }

        public static float WeightAsShare(this Dictionary<ThingDef, int> ores, int weight) {
            float sum = ores.Sum(pair => pair.Value);
            return weight / sum;
        }

    }

}