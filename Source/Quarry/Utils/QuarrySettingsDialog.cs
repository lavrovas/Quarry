using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Quarry.QuarrySettings;

// ReSharper disable once CheckNamespace
namespace Quarry {

    // ReSharper disable once UnusedMember.Global
    public sealed partial class QuarryMod {

        // TODO: Is .Rounded() really needed here?

        private static void DrawBuildingHealthControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf().Rounded();
            Rect controlRectangle = row.RightHalf().Rounded();

            float width = controlRectangle.width;
            float buttonWidth = controlRectangle.height;

            string depletionLabel = QuarrySettings.quarryMaxHealth <= 10000
                ? "QRY_DepletionLabel".Translate(QuarrySettings.quarryMaxHealth.ToString("N0"))
                : "QRY_DepletionLabel".Translate("Infinite");
            Widgets.Label(labelRectangle, depletionLabel);

            // Increment timer value by -100 (button).
            Rect minusButtonRectangle = controlRectangle.LeftPartPixels(buttonWidth);

            bool minusButtonPressed = Widgets.ButtonText(minusButtonRectangle, "-");
            if (minusButtonPressed && QuarrySettings.quarryMaxHealth > 100) {
                QuarrySettings.quarryMaxHealth -= 100;
            }

            Rect sliderRectangle =
                controlRectangle.LeftPartPixels(width - buttonWidth - 10).RightPartPixels(width - 2 * buttonWidth - 10);

            int quarryMaxHealth = Widgets.HorizontalSlider(
                sliderRectangle, QuarrySettings.quarryMaxHealth, 100f, 10100f, true
            ).RoundToAsInt(100);

            QuarrySettings.quarryMaxHealth = quarryMaxHealth;

            // Increment timer value by +100 (button).
            Rect plusButtonRectangle = controlRectangle.RightPartPixels(buttonWidth);

            bool plusButtonPressed = Widgets.ButtonText(plusButtonRectangle, "+");
            if (plusButtonPressed && QuarrySettings.quarryMaxHealth < 10100) {
                QuarrySettings.quarryMaxHealth += 100;
            }

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            // TODO: no tooltip here, why?
        }

        private static void DrawJunkChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf().Rounded();
            Rect sliderRectangle = row.RightHalf().Rounded();

            Widgets.Label(labelRectangle, "QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance));

            int junkChance = Widgets.HorizontalSlider(
                sliderRectangle, QuarrySettings.junkChance, 0f, 100f, true
            ).RoundToAsInt(5);

            QuarrySettings.junkChance = junkChance;

            if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, Static.ToolTipJunkChance);
        }

        private static void DrawChunckChanceControls(Listing listing) {

            Rect row = listing.GetRect(Text.LineHeight);

            Rect labelRectangle = row.LeftHalf().Rounded();
            Rect sliderRectangle = row.RightHalf().Rounded();

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

            Rect letterRectangle = row.LeftHalf().Rounded();

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

            Rect labelRectangle = row.Rounded();

            Widgets.Label(labelRectangle, Static.LabelDictionary);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
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

            OreDictionary.Build();
        }

        private void DrawList(Listing listing, float height) {

            const float rowHeight = 32f;
            const float iconWidth = rowHeight;
            const float inputWidth = 60f;

            Dictionary<ThingDef, int> savedOres = new Dictionary<ThingDef, int>(oreDictionary);
            
            Rect listRectangle = listing.GetRect(height).Rounded();

            Rect cRect = listRectangle.ContractedBy(10f);
            Rect position = new Rect(cRect.x, cRect.y, cRect.width, cRect.height);

            Rect outRectangle = new Rect(0f, 0f, position.width, position.height);
            Rect viewRectangle = new Rect(0f, 0f, position.width - 16f, savedOres.Count * rowHeight);
            

            GUI.BeginGroup(position);
            Widgets.BeginScrollView(outRectangle, ref scrollPosition, viewRectangle);

            var indexedOres = savedOres.Select((pair, index) => new {pair, index});

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
                Widgets.Label(percentRect, $"{savedOres.WeightAsShare(weight):P1}");
                
                weight = Widgets
                    .HorizontalSlider(sliderRect, weight, 0f, OreDictionary.MaxWeight, true)
                    .RoundToAsInt(1);
                
                oreDictionary[definition] = weight;

                if (Mouse.IsOver(row)) {
                    Widgets.DrawHighlight(row);
                }

                TooltipHandler.TipRegion(thingRect, definition.description);

            }

            Widgets.EndScrollView();
            GUI.EndGroup();
            
            // TODO: remove
            Text.Anchor = TextAnchor.UpperLeft;

        }

    }

}