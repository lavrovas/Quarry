using System.Collections.Generic;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    [StaticConstructorOnStartup]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class Building_MiniQuarry : Building_Quarry {

        public override int WallThickness => 1;
        protected override int QuarryDamageMultiplier => 3;
        protected override int SinkholeFrequency => 75;

        protected override IEnumerable<IntVec3> LadderOffsets =>
            new List<IntVec3>() {
                Static.LadderOffset_Small
            };

    }

}