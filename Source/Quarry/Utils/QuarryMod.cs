using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed partial class QuarryMod : Mod {

        private Vector2 scrollPosition = Vector2.zero;

        public QuarryMod(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(GetSettings);
            LongEventHandler.ExecuteWhenFinished(PushDatabase);
            LongEventHandler.ExecuteWhenFinished(BuildDictionary);
        }

        private void GetSettings() {
            GetSettings<QuarrySettings>();
        }

        private void PushDatabase() {
            QuarrySettings.database = DefDatabase<ThingDef>.AllDefsListForReading;
        }

        private static void BuildDictionary() {
            if (QuarrySettings.oreDictionary == null) {
                QuarrySettings.oreDictionary = OreDictionary.Build();
            }
        }

        public override string SettingsCategory() {
            return Static.Quarry;
        }

        public override void DoSettingsWindowContents(Rect rect) {
            Listing_Standard listing = new Listing_Standard {
                ColumnWidth = rect.width
            };

            listing.Begin(rect);

            listing.Gap(10);
            DrawBuildingHealthControls(listing);

            listing.Gap(10);
            DrawJunkChanceControls(listing);

            listing.Gap(10);
            DrawChunckChanceControls(listing);

            // TODO: consider if it is nessessary to allow user resend letter
            listing.Gap(10);
            DrawLetterSendControls(listing);

            listing.Gap(15);
            DrawTableHeader(listing);

            listing.Gap(1);
            DrawButtons(listing);

            listing.Gap(5);
            DrawList(listing, rect.height - listing.CurHeight);

            listing.End();

        }

    }

}