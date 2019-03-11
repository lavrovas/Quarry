using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace Quarry {

    public class JobDriver_MineQuarry : JobDriver {

        private const int BaseTicksBetweenPickHits = 120;
        private const TargetIndex CellInd = TargetIndex.A;
        private const TargetIndex HaulableInd = TargetIndex.B;
        private const TargetIndex StorageCellInd = TargetIndex.C;
        private int ticksToPickHit = -1000;
        private Effecter effecter;
        private Building_Quarry quarryBuilding = null;

        protected Building_Quarry Quarry {
            get {
                if (quarryBuilding == null) {
                    quarryBuilding = job
                        .GetTarget(CellInd).Cell.GetThingList(Map)
                        .Find(q => q is Building_Quarry) as Building_Quarry;
                }

                return quarryBuilding;
            }
        }

        protected Thing Haulable {
            get {
                if (TargetB.Thing != null) {
                    return TargetB.Thing;
                }

                Log.Warning("Quarry:: Trying to assign a null haulable to a pawn.");
                EndJobWith(JobCondition.Errored);
                return null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.Reserve(job.GetTarget(CellInd), job);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            // Set up fail conditions
            this.FailOn(() => Quarry == null || Quarry.IsForbidden(pawn) || Quarry.Depleted);

            // Go to the quarry
            yield return Toils_Goto.Goto(CellInd, PathEndMode.OnCell);

            // Mine at the quarry. This is only for the delay
            yield return Mine();

            // Collect resources from the quarry
            yield return Collect();

            // Reserve the resource
            yield return Toils_Reserve.Reserve(HaulableInd);

            // Reserve the storage cell
            yield return Toils_Reserve.Reserve(StorageCellInd);

            // Go to the resource
            yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch);

            // Pick up the resource
            yield return Toils_Haul.StartCarryThing(HaulableInd);

            // Carry the resource to the storage cell, then place it down
            Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
            yield return carry;
            yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);
        }

        private void ResetTicksToPickHit() {
            float miningSpeed = pawn.GetStatValue(StatDefOf.MiningSpeed);

            // TODO: remove magic number
            if (pawn.Faction != Faction.OfPlayer && miningSpeed < 0.5f) {
                miningSpeed = 0.5f;
            }

            // TODO: remove magic number
            ticksToPickHit = Mathf.RoundToInt(BaseTicksBetweenPickHits / miningSpeed);
        }

        private Toil Mine() {
            return new Toil() {
                tickAction = delegate {
                    
                    pawn.rotationTracker.Face(Quarry.Position.ToVector3Shifted());

                    // TODO: remove magic number
                    if (ticksToPickHit < -100) {
                        ResetTicksToPickHit();
                    }

                    // TODO: remove magic number
                    pawn.skills?.Learn(SkillDefOf.Mining, 0.11f, false);

                    ticksToPickHit--;

                    // TODO: remove magic number
                    if (ticksToPickHit <= 0) {
                        if (effecter == null) {
                            effecter = EffecterDefOf.Mine.Spawn();
                        }

                        effecter.Trigger(pawn, Quarry);

                        ResetTicksToPickHit();
                    }
                },
                defaultDuration = (int) Mathf.Clamp(3000 / pawn.GetStatValue(StatDefOf.MiningSpeed, true), 500, 10000),
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true
            }.WithProgressBarToilDelay(HaulableInd, false, -0.5f);
        }

        private Toil Collect() {
            return new Toil() {
                initAction = delegate {
                    // Increment the record for how many cells this pawn has mined at quarry
                    // TODO: Check nothing broken
                    pawn.records.Increment(QuarryDefOf.QRY_CellsMined);

                    // Use the mineModeToggle to determine the request
                    ResourceRequest requestedResources = Quarry.mineModeToggle
                        ? ResourceRequest.Resources
                        : ResourceRequest.Blocks;

                    // Get the resource from the quarry
                    ThingDef givenResourceDefinition = Quarry.GiveResources(
                        requestedResources, out MoteType mote, out bool singleSpawn, out bool eventTriggered
                    );
                    
                    // If something went wrong, bail out
                    if (givenResourceDefinition?.thingClass == null) {
                        // This shouldn't happen at all, but if it does let's add a little reward
                        // instead of just giving rubble
                        givenResourceDefinition = ThingDefOf.ChunkSlagSteel;
                        mote = MoteType.None;
                        singleSpawn = true;
                        
                        Log.Warning("Quarry:: Tried to quarry mineable ore, but the ore given was null.");
                    }

                    Thing givenResource = ThingMaker.MakeThing(givenResourceDefinition);
                    int stackCount;

                    if (singleSpawn) {
                        stackCount = 1;
                    }
                    else if (givenResourceDefinition == ThingDefOf.ComponentIndustrial) {
                        stackCount = Rand.Range(1, 2);
                    }
                    else {
                        // TODO: Check if BaseMarketValue could be negative, maybe replace with
                        // int sub = (int) Math.Min(givenResourceDefinition.BaseMarketValue / 2f, 10f);
                        int sub = (int) (givenResourceDefinition.BaseMarketValue / 2f);
                        sub = Mathf.Clamp(sub, 0, 10);

                        stackCount = Math.Min(
                            Rand.RangeInclusive(15 - sub, 40 - 2 * sub), givenResourceDefinition.stackLimit
                        );
                    }

                    givenResource.stackCount = stackCount;

                    if (stackCount >= 30) {
                        mote = MoteType.LargeVein;
                    }

                    // Adjust quality for items that use it
                    bool usesQuality = false;
                    CompQuality qualityProperty = givenResource.TryGetComp<CompQuality>();
                    if (qualityProperty != null) {
                        usesQuality = true;
                        qualityProperty.SetQuality(
                            QualityUtility.GenerateQualityTraderItem(), ArtGenerationContext.Outsider
                        );
                    }

                    // Adjust hitpoints, this was just mined from under the ground after all
                    if (givenResourceDefinition.useHitPoints && !givenResourceDefinition.thingCategories.Contains(QuarryDefOf.StoneChunks) &&
                        givenResourceDefinition != ThingDefOf.ComponentIndustrial) {
                        float minHpThresh = 0.25f;
                        if (usesQuality) {
                            minHpThresh = Mathf.Clamp((float) givenResource.TryGetComp<CompQuality>().Quality / 10f,
                                0.1f, 0.7f);
                        }

                        int hp = Mathf.RoundToInt(Rand.Range(minHpThresh, 1f) * givenResource.MaxHitPoints);
                        hp = Mathf.Max(1, hp);
                        givenResource.HitPoints = hp;
                    }

                    // Place the resource near the pawn
                    GenPlace.TryPlaceThing(givenResource, pawn.Position, Map, ThingPlaceMode.Near);

                    // If the resource had a mote, throw it
                    if (mote == MoteType.LargeVein) {
                        MoteMaker.ThrowText(givenResource.DrawPos, Map, Static.TextMote_LargeVein, Color.green, 3f);
                    }
                    else if (mote == MoteType.Failure) {
                        MoteMaker.ThrowText(givenResource.DrawPos, Map, Static.TextMote_MiningFailed, Color.red, 3f);
                    }

                    // If the sinkhole event was triggered, damage the pawn and end this job
                    // Even if the sinkhole doesn't incapacitate the pawn, they will probably want to seek medical attention
                    if (eventTriggered) {
                        NamedArgument pawnName = new NamedArgument(0, pawn.Name.ToStringShort);
                        Messages.Message("QRY_MessageSinkhole".Translate(pawnName), pawn,
                            MessageTypeDefOf.NegativeEvent);
                        DamageInfo dInfo = new DamageInfo(DamageDefOf.Crush, 9,
                            category: DamageInfo.SourceCategory.Collapse);
                        dInfo.SetBodyRegion(BodyPartHeight.Bottom, BodyPartDepth.Inside);
                        pawn.TakeDamage(dInfo);

                        EndJobWith(JobCondition.Succeeded);
                    }
                    else {
                        // Prevent the colonists from trying to haul rubble, which just makes them visit the platform
                        if (givenResourceDefinition == ThingDefOf.Filth_RubbleRock) {
                            EndJobWith(JobCondition.Succeeded);
                        }
                        else {
                            // If this is a chunk or slag, mark it as haulable if allowed to
                            if (givenResourceDefinition.designateHaulable && Quarry.autoHaul) {
                                Map.designationManager.AddDesignation(new Designation(givenResource,
                                    DesignationDefOf.Haul));
                            }

                            // Try to find a suitable storage spot for the resource, removing it from the quarry
                            // If there are no platforms with free space, or if the resource is a chunk, try to haul it to a storage area
                            if (Quarry.autoHaul) {
                                if (!givenResourceDefinition.thingCategories.Contains(QuarryDefOf.StoneChunks) &&
                                    Quarry.HasConnectedPlatform && Quarry.TryFindBestPlatformCell(givenResource, pawn,
                                        Map, pawn.Faction, out IntVec3 c)) {
                                    job.SetTarget(HaulableInd, givenResource);
                                    job.count = givenResource.stackCount;
                                    job.SetTarget(StorageCellInd, c);
                                }
                                else {
                                    StoragePriority currentPriority =
                                        StoreUtility.CurrentStoragePriorityOf(givenResource);
                                    Job result;
                                    if (!StoreUtility.TryFindBestBetterStorageFor(givenResource, pawn, Map,
                                        currentPriority, pawn.Faction, out c, out IHaulDestination haulDestination,
                                        true)) {
                                        JobFailReason.Is("NoEmptyPlaceLower".Translate(), null);
                                    }
                                    else if (haulDestination is ISlotGroupParent) {
                                        result = HaulAIUtility.HaulToCellStorageJob(pawn, givenResource, c, false);
                                    }
                                    else {
                                        job.SetTarget(HaulableInd, givenResource);
                                        job.count = givenResource.stackCount;
                                        job.SetTarget(StorageCellInd, c);
                                    }
                                }
                            }
                            // If there is no spot to store the resource, end this job
                            else {
                                EndJobWith(JobCondition.Succeeded);
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        // TODO: looks like job progress is not saving - no ExposeData()
        
    }

}