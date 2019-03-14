using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed partial class QuarryMod {

        private static void DrawBuildingHealthControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);
            Rect labelRect = row.LeftHalf().Rounded();
            Rect controlsRect = row.RightHalf().Rounded();

            string depletionLabel = QuarrySettings.quarryMaxHealth <= 10000
                ? "QRY_DepletionLabel".Translate(QuarrySettings.quarryMaxHealth.ToString("N0"))
                : "QRY_DepletionLabel".Translate("Infinite");
            Widgets.Label(labelRect, depletionLabel);

            // Increment timer value by -100 (button).
            Rect minusButtonRectangle = new Rect(
                controlsRect.xMin, controlsRect.y, controlsRect.height, controlsRect.height
            );
            bool minusButtonPressed = Widgets.ButtonText(minusButtonRectangle, "-");
            if (minusButtonPressed) {
                if (QuarrySettings.quarryMaxHealth >= 200) {
                    QuarrySettings.quarryMaxHealth -= 100;
                }
            }

            Rect maxHealthSliderRectangle = new Rect(
                controlsRect.xMin + controlsRect.height + 10f,
                controlsRect.y,
                controlsRect.width - controlsRect.height * 2 - 20f,
                controlsRect.height
            );
            QuarrySettings.quarryMaxHealth = Widgets.HorizontalSlider(
                maxHealthSliderRectangle, QuarrySettings.quarryMaxHealth, 100f, 10100f, true
            ).RoundToAsInt(100);

            // Increment timer value by +100 (button).
            Rect plusButtonRectangle = new Rect(
                controlsRect.xMax - controlsRect.height, controlsRect.y, controlsRect.height, controlsRect.height
            );
            bool plusButtonPressed = Widgets.ButtonText(plusButtonRectangle, "+");
            if (plusButtonPressed) {
                if (QuarrySettings.quarryMaxHealth < 10100) {
                    QuarrySettings.quarryMaxHealth += 100;
                }
            }

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            // TODO: no tooltip here, why?
        }

        private static void DrawJunkChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Widgets.Label(row.LeftHalf().Rounded(), "QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance));

            Rect junkSliderOffset = row.RightHalf().Rounded();
            QuarrySettings.junkChance = Widgets.HorizontalSlider(
                junkSliderOffset, QuarrySettings.junkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, Static.ToolTipJunkChance);
        }

        private static void DrawChunckChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);
            Rect labelOffset = row.LeftHalf().Rounded();
            Rect sliderOffset = row.RightHalf().Rounded();

            Widgets.Label(labelOffset, "QRY_SettingsChunkChance".Translate(QuarrySettings.chunkChance));
            QuarrySettings.chunkChance = Widgets.HorizontalSlider(
                sliderOffset, QuarrySettings.chunkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, Static.ToolTipChunkChance);
        }

        private static void DrawLetterSendControls(Listing listing) {

            Rect letterRect = listing.GetRect(Text.LineHeight).LeftHalf().Rounded();

            Widgets.CheckboxLabeled(letterRect, Static.LetterSent, ref QuarrySettings.letterSent);

            if (Mouse.IsOver(letterRect)) {
                Widgets.DrawHighlight(letterRect);
            }

            TooltipHandler.TipRegion(letterRect, Static.ToolTipLetter);
        }

        private static void DrawTableHeader(Listing listing) {

            Rect labelRect = listing.GetRect(32).Rounded();

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(labelRect, Static.LabelDictionary);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawButtons(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight).Rounded();

            // Add an entry to the dictionary
            DrawAddButton(row);

            // Remove an entry from the dictionary
            DrawRemoveButton(row);

            // Reset the dictionary
            DrawResetButton(row);
        }

        private static void DrawAddButton(Rect row) {

            Rect addRect = row.LeftThird();
            if (!Widgets.ButtonText(addRect, Static.LabelAddThing))
                return;

            List<FloatMenuOption> addMenuOptions = QuarryUtility.PossibleThingDefs()
                .Where(thing => !QuarrySettings.oreDictionary.ContainsKey(thing))
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => QuarrySettings.oreDictionary.Add(thing, 1)))
                .ToList();

            FloatMenu menu = new FloatMenu(addMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawRemoveButton(Rect row) {

            Rect removeRect = row.MiddleThird();
            if (!Widgets.ButtonText(
                    removeRect, Static.LabelRemoveThing, active: QuarrySettings.oreDictionary.Count >= 2
                )
            )
                return;

            List<FloatMenuOption> removeMenuOptions = QuarrySettings.oreDictionary
                .Select(pair => pair.Key)
                .OrderBy(thing => thing.label)
                .Select(thing =>
                    new FloatMenuOption(thing.LabelCap, () => QuarrySettings.oreDictionary.Remove(thing)))
                .ToList();

            FloatMenu menu = new FloatMenu(removeMenuOptions);
            Find.WindowStack.Add(menu);
        }

        private static void DrawResetButton(Rect row) {

            Rect resetRect = row.RightThird();
            if (Widgets.ButtonText(resetRect, Static.LabelResetList)) {
                OreDictionary.Build();
            }
        }

        private void DrawTable(Listing listing, float height) {

            const float rowHeight = 32f;
            const float iconWidth = rowHeight;
            const float percentWidth = 40f;

            Rect listRect = listing.GetRect(height).Rounded();

            Rect position = listRect.ContractedBy(10f);
            // Rect position = new Rect(cRect.x, cRect.y, cRect.width, cRect.height);

            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

            GUI.BeginGroup(position);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Dictionary<ThingDef, int> oreDictionary = QuarrySettings.oreDictionary;
            var indexedOres = oreDictionary.Select((pair, index) => new {pair, index});
            foreach (var ore in indexedOres) {

                int index = ore.index;
                ThingDef definition = ore.pair.Key;
                int weight = ore.pair.Value;
                string uselessString = null;


                Rect row = new Rect(0f, rowHeight * index, viewRect.width, rowHeight);

                Rect thingRect = row.LeftThird();
                Rect iconRect = thingRect.LeftPartPixels(iconWidth);
                Rect labelRect = thingRect.RightPartPixels(thingRect.width - iconWidth - 1);

                Rect weightRect = row.MiddleThird().LeftHalf();
                Rect inputRect = weightRect.LeftPartPixels(weightRect.width - percentWidth - 1);
                Rect percentRect = weightRect.RightPartPixels(percentWidth);

                Rect sliderRect = row.RightHalf();


                Widgets.ThingIcon(iconRect, definition);
                Widgets.Label(labelRect, definition.LabelCap);
                Widgets.TextFieldNumeric(inputRect, ref weight, ref uselessString, 1, OreDictionary.MaxWeight);
                Widgets.Label(percentRect, $"{oreDictionary.WeightAsShare(weight):P}");

                weight = Widgets
                    .HorizontalSlider(sliderRect, weight, 0f, OreDictionary.MaxWeight, true)
                    .RoundToAsInt(1);

                oreDictionary[definition] = weight;

                if (Mouse.IsOver(row)) {
                    Widgets.DrawHighlight(row);
                }

                TooltipHandler.TipRegion(thingRect, definition.description);

                scrollViewHeight += rowHeight * index;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

    }

}