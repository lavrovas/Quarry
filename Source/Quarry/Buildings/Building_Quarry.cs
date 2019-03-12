using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    public enum ResourceRequest {

        None,
        Resources,
        Blocks

    }

    public enum MoteType {

        None,
        LargeVein,
        Failure

    }

    // ReSharper disable once InconsistentNaming
    [StaticConstructorOnStartup]
    public class Building_Quarry : Building, IAssignableBuilding {

        public bool AutoHaul = true;
        public bool MineModeToggle = true;

        private float quarryPercent = 1f;
        private int jobsCompleted;
        private bool firstSpawn;
        private CompAffectedByFacilities facilityComp;
        private List<string> rockTypesUnder = new List<string>();
        private List<ThingDef> blocksUnder = new List<ThingDef>();
        private List<ThingDef> chunksUnder = new List<ThingDef>();
        private List<Pawn> owners = new List<Pawn>();

        public virtual int WallThickness => 2;
        public bool Unowned => owners.Count <= 0;
        public bool Depleted => QuarryPercent <= 0;

        public IEnumerable<Pawn> AssigningCandidates => Spawned ? Map.mapPawns.FreeColonists : Enumerable.Empty<Pawn>();

        public IEnumerable<Pawn> AssignedPawns => owners;

        public int MaxAssignedPawnsCount => Spawned ? this.OccupiedRect().ContractedBy(WallThickness).Cells.Count() : 0;

        public bool AssignedAnything(Pawn pawn) {
            return false;
        }

        private float QuarryPercent {
            get {
                if (QuarrySettings.QuarryMaxHealth == int.MaxValue) {
                    return 100f;
                }

                return quarryPercent * 100f;
            }
        }

        private IEnumerable<ThingDef> ChunksUnder {
            get {
                if (chunksUnder.Count <= 0) {
                    MakeThingDefListsFrom(RockTypesUnder);
                }

                return chunksUnder;
            }
        }

        private IEnumerable<ThingDef> BlocksUnder {
            get {
                if (blocksUnder.Count <= 0) {
                    MakeThingDefListsFrom(RockTypesUnder);
                }

                return blocksUnder;
            }
        }

        protected virtual int QuarryDamageMultiplier => 1;
        protected virtual int SinkholeFrequency => 100;

        protected virtual IEnumerable<IntVec3> LadderOffsets =>
            new List<IntVec3> {
                Static.LadderOffset_Big1,
                Static.LadderOffset_Big2,
                Static.LadderOffset_Big3,
                Static.LadderOffset_Big4
            };

        private bool PlayerCanSeeOwners {
            get {
                return Faction == Faction.OfPlayer || owners.Any(
                           pawn => pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer
                       );
            }
        }

        private IEnumerable<string> RockTypesUnder {
            get {
                if (rockTypesUnder.Count <= 0) {
                    rockTypesUnder = RockTypesFromMap();
                }

                return rockTypesUnder;
            }
        }

        public override void ExposeData() {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving) {
                owners.RemoveAll(pawn => pawn.Destroyed);
            }

            Scribe_Values.Look(ref AutoHaul, "QRY_boolAutoHaul", true);
            Scribe_Values.Look(ref MineModeToggle, "QRY_mineMode", true);
            Scribe_Values.Look(ref quarryPercent, "QRY_quarryPercent", 1f);
            Scribe_Values.Look(ref jobsCompleted, "QRY_jobsCompleted", 0);
            Scribe_Collections.Look(ref rockTypesUnder, "QRY_rockTypesUnder", LookMode.Value);
            Scribe_Collections.Look(ref owners, "owners", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                SortOwners();
            }
        }

        public override void PostMake() {
            base.PostMake();
            firstSpawn = true;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);

            facilityComp = GetComp<CompAffectedByFacilities>();

            if (!firstSpawn)
                return;

            // Set the initial quarry health
            quarryPercent = 1f;

            CellRect buildingArea = this.OccupiedRect();

            // Remove this area from the quarry grid. Quarries can never be built here again
            map.GetComponent<QuarryGrid>().RemoveFromGrid(buildingArea);

            foreach (IntVec3 cell in buildingArea) {
                // What type of terrain are we over?
                string rockType = cell.GetTerrain(Map).defName.Split('_').First();

                // If this is a valid rock type, add it to the list
                if (QuarryUtility.IsValidQuarryRock(rockType)) {
                    rockTypesUnder.Add(rockType);
                }

                // Change the terrain here to be quarried stone					
                if (buildingArea.ContractedBy(WallThickness).Contains(cell)) {
                    Map.terrainGrid.SetTerrain(cell, QuarryDefOf.QRY_QuarriedGround);
                }
                else {
                    Map.terrainGrid.SetTerrain(cell, QuarryDefOf.QRY_QuarriedGroundWall);
                }
            }

            // Now that all the cells have been processed, create ThingDef lists
            MakeThingDefListsFrom(RockTypesUnder);

            // Spawn filth for the quarry
            foreach (IntVec3 cell in buildingArea) {
                SpawnFilth(cell);
            }

            // Change the ground back to normal quarried stone where the ladders are
            // This is to negate the speed decrease and encourages pawns to use the ladders
            foreach (IntVec3 offset in LadderOffsets) {
                Map.terrainGrid.SetTerrain(Position + offset.RotatedBy(Rotation), QuarryDefOf.QRY_QuarriedGround);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
            RemoveAllOwners();

            // Change the terrain here back to quarried stone, removing the walls
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(this)) {
                Map.terrainGrid.SetTerrain(cell, QuarryDefOf.QRY_QuarriedGround);
            }

            if (!QuarrySettings.letterSent && !TutorSystem.AdaptiveTrainingEnabled) {
                Find.LetterStack.ReceiveLetter(
                    Static.LetterLabel, Static.LetterText, QuarryDefOf.CuproLetter, new GlobalTargetInfo(Position, Map)
                );
                
                QuarrySettings.letterSent = true;
            }

            if (TutorSystem.AdaptiveTrainingEnabled) {
                LessonAutoActivator.TeachOpportunity(QuarryDefOf.QRY_ReclaimingSoil, OpportunityType.GoodToKnow);
            }

            base.Destroy(mode);
        }

        public void TryAssignPawn(Pawn pawn) {
            if (!owners.Contains(pawn)) {
                owners.Add(pawn);
            }
        }

        public void TryUnassignPawn(Pawn pawn) {
            if (owners.Contains(pawn)) {
                owners.Remove(pawn);
            }
        }

        private void SortOwners() {
            owners.SortBy(owner => owner.thingIDNumber);
        }

        private void RemoveAllOwners() {
            owners.Clear();
        }

        private List<string> RockTypesFromMap() {
            // Try to add all the rock types found in the map
            List<string> list = new List<string>();

            // TODO: check if LabelCap not affected by localization
            List<string> tempRockTypesUnder =
                Find.World.NaturalRockTypesIn(Map.Tile).Select(rockType => rockType.LabelCap).ToList();

            foreach (string rockType in tempRockTypesUnder) {
                if (QuarryUtility.IsValidQuarryRock(rockType)) {
                    list.Add(rockType);
                }
            }

            if (list.Count > 0)
                return list;

            // This will cause an error if there still isn't a list, so make a new one using known rocks
            Log.Warning("Quarry:: No valid rock types were found in the map. Building list using vanilla rocks.");
            list = new List<string> {"Sandstone", "Limestone", "Granite", "Marble", "Slate"};

            return list;
        }

        private void MakeThingDefListsFrom(IEnumerable<string> names) {
            chunksUnder = new List<ThingDef>();
            blocksUnder = new List<ThingDef>();

            foreach (string name in names) {
                if (QuarryUtility.IsValidQuarryChunk(name, out ThingDef chunk)) {
                    chunksUnder.Add(chunk);
                }

                if (QuarryUtility.IsValidQuarryBlocks(name, out ThingDef block)) {
                    blocksUnder.Add(block);
                }
            }
        }

        private void SpawnFilth(IntVec3 cell) {
            // Skip this cell if it is occupied by a placed object
            // This is to avoid save compression errors

            List<Thing> thingsInCell = Map.thingGrid.ThingsListAtFast(cell);
            if (thingsInCell.Any(thing => thing.def.saveCompressible)) {
                return;
            }

            int filthAmount = Rand.RangeInclusive(1, 100);

            // If this cell isn't filthy enough, skip it
            if (filthAmount <= 20) {
                return;
            }

            // Check for dirt filth
            if (filthAmount <= 40) {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Filth_Dirt), cell, Map);
            }
            else {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Filth_RubbleRock), cell, Map);

                // Check for chunks
                if (filthAmount > 80) {
                    GenSpawn.Spawn(ThingMaker.MakeThing(ChunksUnder.RandomElement()), cell, Map);
                }
            }
        }

        public ThingDef GiveResources(
            ResourceRequest resourceRequested, out MoteType mote, out bool singleSpawn, out bool eventTriggered
        ) {
            // Increment the jobs completed
            jobsCompleted++;

            mote = MoteType.None;
            singleSpawn = true;
            eventTriggered = false;

            // Decrease the amount this quarry can be mined, eventually depleting it
            if (QuarrySettings.QuarryMaxHealth != int.MaxValue) {
                QuarryMined();
            }

            // Determine if the mining job resulted in a sinkhole event, based on game difficulty
            if (jobsCompleted % SinkholeFrequency == 0 && Rand.Chance(Find.Storyteller.difficulty.difficulty / 50f)) {
                eventTriggered = true;

                // The sinkhole damages the quarry a little
                QuarryMined(Rand.RangeInclusive(1, 3));
            }

            // Cache values since this process is convoluted and the values need to remain the same
            bool junkMined = Rand.Chance(QuarrySettings.junkChance / 100f);
            bool chunkMined = Rand.Chance(QuarrySettings.chunkChance / 100f);

            switch (resourceRequested) {
                // Check for blocks first to prevent spawning chunks (these would just be cut into blocks)

                // The rock didn't break into a usable size, spawn rubble
                case ResourceRequest.Blocks when junkMined:
                    mote = MoteType.Failure;
                    return ThingDefOf.Filth_RubbleRock;

                // Spawn block
                case ResourceRequest.Blocks:
                    singleSpawn = false;
                    return BlocksUnder.RandomElement();

                // Try to give junk before resources. This simulates only mining chunks or useless rubble
                case ResourceRequest.Resources when junkMined && !chunkMined: {
                    mote = MoteType.Failure;
                    return ThingDefOf.Filth_RubbleRock;
                }

                // Spawn chunk
                case ResourceRequest.Resources when junkMined: {
                    return ChunksUnder.RandomElement();
                }

                // Spawn resource
                case ResourceRequest.Resources:
                    singleSpawn = false;
                    return OreDictionary.TakeOne();

                // The quarry was most likely toggled off while a pawn was still working. Give junk
                case ResourceRequest.None:
                    return ThingDefOf.Filth_RubbleRock;

                default:
                    return ThingDefOf.Filth_RubbleRock;
            }

        }

        private void QuarryMined(int damage = 1) {
            // TODO: too complicated, best is to save currentHealth
            // ??? problem - settings change will affect it then
            quarryPercent = (QuarrySettings.quarryMaxHealth * quarryPercent - damage * QuarryDamageMultiplier) /
                            QuarrySettings.quarryMaxHealth;
        }

        public bool TryFindBestPlatformCell(Thing thing, Pawn carrier, Map map, Faction faction,
            out IntVec3 foundCell) {

            IEnumerable<Thing> allowedStorages = facilityComp.LinkedFacilitiesListForReading
                .Where(facilities => facilities.GetSlotGroup()?.Settings.AllowedToAccept(thing) == true);

            foreach (Thing storage in allowedStorages) {
                foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(storage)) {
                    if (!StoreUtility.IsGoodStoreCell(cell, map, thing, carrier, faction))
                        continue;

                    foundCell = cell;
                    return true;
                }
            }

            foundCell = IntVec3.Invalid;
            return false;
        }

        public override IEnumerable<Gizmo> GetGizmos() {

            // TODO: maybe this should be Command_Toggle
            Command_Action mineMode = new Command_Action {
                icon = MineModeToggle ? Static.DesignationQuarryResources : Static.DesignationQuarryBlocks,
                defaultLabel = MineModeToggle ? Static.LabelMineResources : Static.LabelMineBlocks,
                defaultDesc = MineModeToggle ? Static.DescriptionMineResources : Static.DescriptionMineBlocks,
                hotKey = KeyBindingDefOf.Misc10,
                activateSound = SoundDefOf.Click,
                action = () => { MineModeToggle = !MineModeToggle; },
            };

            // Only allow this option if stonecutting has been researched
            // The default behavior is to allow resources, but not blocks

            // TODO: never saw this working
            if (!QuarryDefOf.Stonecutting.IsFinished) {
                mineMode.Disable(Static.ReportGizmoLackingResearch);
            }

            yield return mineMode;

            yield return new Command_Toggle {
                icon = Static.DesignationHaul,
                defaultLabel = Static.LabelHaulMode,
                defaultDesc = AutoHaul ? Static.LabelHaul : Static.LabelNotHaul,
                hotKey = KeyBindingDefOf.Misc11,
                activateSound = SoundDefOf.Click,
                isActive = () => AutoHaul,
                toggleAction = () => { AutoHaul = !AutoHaul; }
            };

            yield return new Command_Action {
                defaultLabel = Static.CommandBedSetOwnerLabel,
                icon = Static.DesignationSetOwners,
                defaultDesc = Static.CommandSetOwnerDesc,
                action = () => Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this)),
                hotKey = KeyBindingDefOf.Misc3
            };

            foreach (Gizmo baseGizmo in base.GetGizmos()) {
                yield return baseGizmo;
            }
        }

        public override string GetInspectString() {
            const int maxNamesToShow = 3;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Static.InspectQuarryPercent + ": " + QuarryPercent.ToStringDecimalIfSmall() + "%");

            if (!PlayerCanSeeOwners)
                return stringBuilder.ToString();

            stringBuilder.AppendLine("ForColonistUse".Translate());

            switch (owners.Count) {
                case 0:
                    stringBuilder.AppendLine("Owner".Translate() + ": " + "Nobody".Translate().ToLower());
                    break;
                case 1:
                    stringBuilder.AppendLine("Owner".Translate() + ": " + owners[0].Label);
                    break;
                default: {
                    string[] ownerNames = owners.Take(maxNamesToShow).Select(owner => owner.LabelShort).ToArray();
                    stringBuilder.Append("Owners".Translate() + ": " + string.Join(", ", ownerNames));

                    if (owners.Count > maxNamesToShow) {
                        stringBuilder.Append($" (+ {owners.Count - maxNamesToShow})");
                    }

                    stringBuilder.AppendLine();
                    break;
                }
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

    }

}