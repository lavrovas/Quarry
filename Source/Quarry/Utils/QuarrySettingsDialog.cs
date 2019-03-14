using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Quarry.QuarrySettings;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed partial class QuarryMod {

        private static void DrawBuildingHealthControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf();
            Rect controlRectangle = row.RightHalf();

            float width = controlRectangle.width;
            float buttonWidth = controlRectangle.height;

            string depletionLabel = quarryMaxHealth <= 10000
                ? "QRY_DepletionLabel".Translate(quarryMaxHealth.ToString("N0"))
                : "QRY_DepletionLabel".Translate("Infinite");
            Widgets.Label(labelRectangle, depletionLabel);

            // Increment timer value by -100 (button).
            Rect minusButtonRectangle = controlRectangle.LeftPartPixels(buttonWidth);

            bool minusButtonPressed = Widgets.ButtonText(minusButtonRectangle, "-");
            if (minusButtonPressed && quarryMaxHealth > 100) {
                quarryMaxHealth -= 100;
            }

            Rect sliderRectangle =
                controlRectangle.LeftPartPixels(width - buttonWidth - 10).RightPartPixels(width - 2 * buttonWidth - 10);

            quarryMaxHealth = Widgets.HorizontalSlider(
                sliderRectangle, quarryMaxHealth, 100f, 10100f, true
            ).RoundToAsInt(100);

            // Increment timer value by +100 (button).
            Rect plusButtonRectangle = controlRectangle.RightPartPixels(buttonWidth);

            bool plusButtonPressed = Widgets.ButtonText(plusButtonRectangle, "+");
            if (plusButtonPressed && quarryMaxHealth < 10100) {
                quarryMaxHealth += 100;
            }

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            // TODO: no tooltip here, why?
        }

        private static void DrawJunkChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf();
            Rect sliderRectangle = row.RightHalf();

            Widgets.Label(labelRectangle, "QRY_SettingsJunkChance".Translate(junkChance));

            junkChance = Widgets.HorizontalSlider(
                sliderRectangle, junkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, Static.ToolTipJunkChance);
        }

        private static void DrawChunkChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf();
            Rect sliderRectangle = row.RightHalf();

            Widgets.Label(labelRectangle, "QRY_SettingsChunkChance".Translate(chunkChance));
            chunkChance = Widgets.HorizontalSlider(
                sliderRectangle, chunkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, Static.ToolTipChunkChance);
        }

        private static void DrawLetterSendControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect letterRectangle = row.LeftHalf();

            Widgets.CheckboxLabeled(letterRectangle, Static.LetterSent, ref letterSent);

            if (Mouse.IsOver(letterRectangle)) {
                Widgets.DrawHighlight(letterRectangle);
            }

            TooltipHandler.TipRegion(letterRectangle, Static.ToolTipLetter);
        }

        private static void DrawTableHeader(Listing listing) {

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row;

            Widgets.Label(labelRectangle, Static.LabelDictionary);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawButtons(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            // Add an entry to the dictionary
            DrawAddButton(row);

            // Remove an entry from the dictionary
            DrawRemoveButton(row);

            // Reset the dictionary
            DrawResetButton(row);
        }

        private static void DrawAddButton(Rect row) {

            Rect buttonRectangle = row.LeftThird();

            if (!Widgets.ButtonText(buttonRectangle, Static.LabelAddThing))
                return;

            List<FloatMenuOption> addMenuOptions = QuarryUtility.PossibleThingDefs()
                .Where(thing => !oreDictionary.ContainsKey(thing))
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => oreDictionary.Add(thing, 1)))
                .ToList();

            FloatMenu menu = new FloatMenu(addMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawRemoveButton(Rect row) {

            Rect buttonRectangle = row.MiddleThird();
            bool removeAllowed = oreDictionary.Count > 1;

            if (!Widgets.ButtonText(buttonRectangle, Static.LabelRemoveThing, active: removeAllowed))
                return;

            List<FloatMenuOption> removeMenuOptions = oreDictionary
                .Select(pair => pair.Key)
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => oreDictionary.Remove(thing)))
                .ToList();

            FloatMenu menu = new FloatMenu(removeMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawResetButton(Rect row) {

            Rect buttonRectangle = row.RightThird();

            if (!Widgets.ButtonText(buttonRectangle, Static.LabelResetList))
                return;

            oreDictionary = OreDictionary.Build();
        }

        private void DrawList(Listing listing, float height) {

            const float rowHeight = 32f;
            const float iconWidth = rowHeight;
            const float inputWidth = 60f;


            Rect listRectangle = listing.GetRect(height);

            Rect position = listRectangle.ContractedBy(10f);
            Rect outRectangle = new Rect(0f, 0f, position.width, position.height);
            Rect viewRectangle = new Rect(0f, 0f, position.width - 16f, oreDictionary.Count * rowHeight);


            GUI.BeginGroup(position);
            Widgets.BeginScrollView(outRectangle, ref scrollPosition, viewRectangle);

            Text.Anchor = TextAnchor.MiddleLeft;
            
            var indexedOres = new Dictionary<ThingDef, int>(oreDictionary).Select((pair, index) => new {pair, index});
            foreach (var indexed in indexedOres) {

                int index = indexed.index;
                ThingDef definition = indexed.pair.Key;
                int weight = indexed.pair.Value;

                string buffer = null;

                Rect row = new Rect(0f, rowHeight * index, viewRectangle.width, rowHeight);

                Rect thingRect = row.LeftThird();
                Rect iconRect = thingRect.LeftPartPixels(iconWidth);
                Rect labelRect = thingRect.RightPartPixels(thingRect.width - iconWidth - 10);

                Rect weightRect = row.MiddleThird().LeftHalf();
                Rect inputRect = weightRect.LeftPartPixels(inputWidth);
                Rect percentRect = weightRect.RightPartPixels(weightRect.width - inputWidth - 10);

                Rect sliderRect = row.RightHalf();

                Widgets.ThingIcon(iconRect, definition);
                Widgets.Label(labelRect, definition.LabelCap);
                Widgets.TextFieldNumeric(inputRect, ref weight, ref buffer, 1, OreDictionary.MaxWeight);
                Widgets.Label(percentRect, $"{oreDictionary.WeightAsShare(weight):P1}");

                oreDictionary[definition] = Widgets
                    .HorizontalSlider(sliderRect, weight, 0f, OreDictionary.MaxWeight, true)
                    .RoundToAsInt(1);

                if (Mouse.IsOver(row)) {
                    Widgets.DrawHighlight(row);
                }

                TooltipHandler.TipRegion(thingRect, definition.description);
            }

            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

    }

}