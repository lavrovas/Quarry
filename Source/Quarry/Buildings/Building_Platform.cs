using UnityEngine;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class Building_Platform : Building_Storage {

        private Graphic cachedGraphic = null;

        // Graphic_Appearances needs an atlased texture to function properly,
        // and rewriting it to use single textures didn't work as expected. Thus:
        public override Graphic Graphic {
            get {
                if (cachedGraphic == null)
                    cachedGraphic = PrepareColoredGraphics();

                return cachedGraphic;
            }
        }

        private Graphic PrepareColoredGraphics() {
            Color colorOne = def.graphicData.color;
            Color colorTwo = def.graphicData.colorTwo;
            Graphic graphic = Static.Platform_Smooth;

            if (Stuff?.stuffProps != null) {
                colorOne = Stuff.stuffProps.color;

                if (Stuff.stuffProps.appearance == QuarryDefOf.Bricks) {
                    graphic = Static.Platform_Bricks;
                }
                else if (Stuff.stuffProps.appearance == QuarryDefOf.Planks) {
                    graphic = Static.Platform_Planks;
                }
                else if (Stuff.stuffProps.appearance?.defName == Static.StringGraniticStone) {
                    graphic = Static.Platform_GraniticStone;
                }
                else if (Stuff.stuffProps.appearance?.defName == Static.StringRockyStone) {
                    graphic = Static.Platform_RockyStone;
                }
                else if (Stuff.stuffProps.appearance?.defName == Static.StringSmoothStone) {
                    graphic = Static.Platform_SmoothStone;
                }
            }

            Graphic coloredGraphics = graphic.GetColoredVersion(ShaderDatabase.DefaultShader, colorOne, colorTwo);
            return coloredGraphics;
        }

        // This is to correct an issue between versions and can be removed in B19
        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            if (settings == null) {
                settings = new StorageSettings(this);
                if (def.building.defaultStorageSettings != null) {
                    settings.CopyFrom(def.building.defaultStorageSettings);
                }
            }

            base.SpawnSetup(map, respawningAfterLoad);
        }

    }

}