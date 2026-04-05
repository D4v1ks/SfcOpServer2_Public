#pragma warning disable IDE1006

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace shrQ3
{
    public sealed class clsUI
    {
        private const int bufferSize = 1 << 18;

        [Flags]
        public enum eFlags
        {
            SortGeneric = 1 << 0,
            SortLayouts = 1 << 1,

            ImportLayouts = 1 << 2,
            ExportLayouts = 1 << 3,

            DeleteLayout = 1 << 4,
            IsolateLayout = 1 << 5,
        }

        public enum eFunction
        {
            None,

            Count,
            Remap,
            Test
        }

        public static readonly string[] Races =
        [
            "FF",
            "KK",
            "RR",
            "LL",
            "HH",
            "GG",
            "II",
            "ZZ",
            "PP",

            "XX",
            "YY",
        ];

        public static readonly string[] Special =
        [
            "Asteroid",
            "Drone",
            "Planet",
            "Plasma",
            "Pulsar",
            "Star"
        ];

        public static readonly string[] Shields =
        [
            "Shield4",
            "Reinf4",
            "Tonk4",
            "Shield5",
            "Reinf5",
            "Tonk5",
            "Shield6",
            "Reinf6",
            "Tonk6",
            "Shield1",
            "Reinf1",
            "Tonk1",
            "Shield2",
            "Reinf2",
            "Tonk2",
            "Shield3",
            "Reinf3",
            "Tonk3"
        ];

        public static readonly string[] Systems =
        [
            "INT_SENSORS",
            "INT_SHUTTLE",
            "INT_TRACTOR",
            "INT_TRANSPORTER",
            "WEAPONS_1",
            "WEAPONS_10",
            "WEAPONS_11",
            "WEAPONS_12",
            "WEAPONS_13",
            "WEAPONS_14",
            "WEAPONS_15",
            "WEAPONS_16",
            "WEAPONS_17",
            "WEAPONS_18",
            "WEAPONS_19",
            "WEAPONS_2",
            "WEAPONS_20",
            "WEAPONS_21",
            "WEAPONS_22",
            "WEAPONS_23",
            "WEAPONS_24",
            "WEAPONS_25", // hide hardpoint #25
            "WEAPONS_3",
            "WEAPONS_4",
            "WEAPONS_5",
            "WEAPONS_6",
            "WEAPONS_7",
            "WEAPONS_8",
            "WEAPONS_9"
        ];

        public static readonly string[] Weapons =
        [
            "WEAPONS_1",
            "WEAPONS_10",
            "WEAPONS_11",
            "WEAPONS_12",
            "WEAPONS_13",
            "WEAPONS_14",
            "WEAPONS_15",
            "WEAPONS_16",
            "WEAPONS_17",
            "WEAPONS_18",
            "WEAPONS_19",
            "WEAPONS_2",
            "WEAPONS_20",
            "WEAPONS_21",
            "WEAPONS_22",
            "WEAPONS_23",
            "WEAPONS_24",
            "WEAPONS_25", // hide hardpoint #25
            "WEAPONS_3",
            "WEAPONS_4",
            "WEAPONS_5",
            "WEAPONS_6",
            "WEAPONS_7",
            "WEAPONS_8",
            "WEAPONS_9"
        ];

        public static void Process(clsQ3 source, clsQ3 layouts = null, eFlags flags = eFlags.SortGeneric | eFlags.SortLayouts, string mask = null, int lastHardcodedId = int.MaxValue)
        {
            Contract.Assert(
                source != null &&
                uint.PopCount((uint)(flags & (eFlags.SortGeneric | eFlags.SortLayouts))) > 0 &&
                uint.PopCount((uint)(flags & (eFlags.ImportLayouts | eFlags.ExportLayouts))) < 2 &&
                uint.PopCount((uint)(flags & (eFlags.DeleteLayout | eFlags.IsolateLayout))) < 2 &&
                (mask == null || mask.Length != 0)
            );

#if DEBUG
            tObject.Parent = null;
#endif

            int[] buffer = ArrayPool<int>.Shared.Rent(bufferSize * 3);

            if ((flags & eFlags.ImportLayouts) == eFlags.ImportLayouts)
                Append(source, layouts, buffer);

            Sort(source, flags, mask, lastHardcodedId, buffer, out SortedDictionary<string, tAsset>[] temporary);
            Rebuild(source, buffer, temporary);

            ArrayPool<int>.Shared.Return(buffer);
        }

        // process functions

        private static void Append(clsQ3 source, clsQ3 layouts, int[] buffer)
        {
            Contract.Assert(layouts != null && layouts.Assets.Count != 0);

            PullBack(layouts, buffer, bufferSize);

            foreach (KeyValuePair<int, tAsset> p in layouts.Assets)
                source.Assets.Add(p.Key, p.Value);

            foreach (KeyValuePair<string, tSprite> p in layouts.Sprites)
                source.Sprites.TryAdd(p.Key, p.Value);

            layouts.Clear();
        }

        private static void PullBack(clsQ3 source, int[] buffer, int floorId)
        {
            Dictionary<int, tAsset> d = new(source.Assets.Count);

            Array.Clear(buffer);

            int newId;

            foreach (KeyValuePair<int, tAsset> p in source.Assets)
            {
                tAsset asset = p.Value;

                Contract.Assert(asset.Id == p.Key);

                newId = asset.Id + floorId;

                Contract.Assert(buffer[asset.Id] == 0 && buffer[newId] == 0);

                buffer[asset.Id] = newId;
                buffer[newId] = newId;

                asset.Id = newId;

                d.Add(newId, asset);
            }

            source.Assets.Clear();

            foreach (KeyValuePair<int, tAsset> p in d)
                source.Assets.Add(p.Key, p.Value);

            d.Clear();

            Map(source, buffer, eFunction.Remap);
        }

        private static void Sort(clsQ3 source, eFlags flags, string mask, int lastHardcodedId, int[] buffer, out SortedDictionary<string, tAsset>[] temporary)
        {
            // counts the number of times each asset is referenced by other assets in the source

            Array.Clear(buffer);

            Map(source, buffer, eFunction.Count);

            // tries to process the mask we will use to sort the assets

            uint comparison;
            string[] masks;

            if (mask == null)
            {
                comparison = 0;
                masks = null;
            }
            else
            {
                if (mask.StartsWith('!'))
                {
                    if (mask.EndsWith('!'))
                        comparison = 1;
                    else
                        comparison = 2;
                }
                else if (mask.EndsWith('!'))
                    comparison = 3;
                else
                    comparison = 4;

                masks = mask.Split('!', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                Contract.Assert(masks.Length != 0);
            }

            // creates the temporary lists

            temporary = new SortedDictionary<string, tAsset>[9];

            for (int i = 0; i < temporary.Length; i++)
                temporary[i] = [];

            // starts sorting everything

            foreach (KeyValuePair<int, tAsset> p in source.Assets)
            {
                int id = p.Key;

                if (buffer[id] > 0)
                    continue;

                tAsset asset = p.Value;

                if (asset.Type != tAsset.eType.Scene && asset.Type != tAsset.eType.RadioGroup)
                    continue;

                string t = asset.Name.Value;

                if (t.Length == 0)
                    throw new NotSupportedException();

                string k;
                int i;

                if (t.StartsWith("VERTICALS_", StringComparison.Ordinal))
                {
                    k = t[10..];
                    i = 4;
                }
                else if (t.StartsWith("SYSTEMS_", StringComparison.Ordinal))
                {
                    k = t[8..];
                    i = 5;
                }
                else if (t.StartsWith("HORIZONTALS_", StringComparison.Ordinal))
                {
                    k = t[12..];
                    i = 6;
                }
                else if (t.StartsWith("WEAPONS_", StringComparison.Ordinal))
                {
                    if (t.EndsWith("_VL", StringComparison.Ordinal))
                    {
                        k = t[8..^3];
                        i = 3;
                    }
                    else
                    {
                        k = t[8..];
                        i = 8;
                    }
                }
                else if (t.Contains("_HULL_"))
                {
                    if (t.EndsWith("_VL", StringComparison.Ordinal))
                    {
                        k = t[8..^3];
                        i = 2;
                    }
                    else
                    {
                        k = t[8..];
                        i = 7;
                    }
                }
                else
                {
                    // tries to process the generic asset

                    if ((flags & eFlags.SortGeneric) == eFlags.SortGeneric)
                    {
                        if (id <= lastHardcodedId)
                        {
                            k = $"{id:D8}";

                            temporary[0].Add(k, asset);
                        }
                        else
                            temporary[1].Add(t, asset);
                    }

                    continue;
                }

                // tries to process the layout asset

                if ((flags & eFlags.DeleteLayout) == eFlags.DeleteLayout)
                {
                    if (Matches(k, comparison, masks))
                        continue;
                }

                if ((flags & eFlags.IsolateLayout) == eFlags.IsolateLayout)
                {
                    if (!Matches(k, comparison, masks))
                        continue;
                }

                if ((flags & eFlags.SortLayouts) == eFlags.SortLayouts)
                {
                    Contract.Assert(i >= 2);

                    temporary[i].Add(t, asset);
                }
            }
        }

        private static bool Matches(string k, uint comparison, string[] masks)
        {
            // !FCA!     -> matches any layout named "FCA"
            // !FCA!FF!  -> matches any layout named "FCA" or "FF"
            // !FC       -> matches any layout that starts with "FC"
            // !FC!FD    -> matches any layout that starts with "FC" or "FD"
            // CA!       -> matches any layout that ends with "CA"
            // CA!DD!    -> matches any layout that ends with "CA" or "DD"
            // CA        -> matches any layout that contains "CA"
            // CA!FF     -> matches any layout that contains "CA" or "FF"

            switch (comparison)
            {
                case 1:
                    foreach (string m in masks)
                    {
                        if (k.Equals(m, StringComparison.Ordinal))
                            return true;
                    }

                    break;

                case 2:
                    foreach (string m in masks)
                    {
                        if (k.StartsWith(m, StringComparison.Ordinal))
                            return true;
                    }

                    break;

                case 3:
                    foreach (string m in masks)
                    {
                        if (k.EndsWith(m, StringComparison.Ordinal))
                            return true;
                    }

                    break;

                case 4:
                    foreach (string m in masks)
                    {
                        if (k.Contains(m, StringComparison.Ordinal))
                            return true;
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            return false;
        }

        private static void Rebuild(clsQ3 source, int[] buffer, SortedDictionary<string, tAsset>[] temporary)
        {
            clsQ3 draft = new(0);

            source.Update();

            for (int i = 0; i < temporary.Length; i++)
            {
                if (temporary[i].Count != 0)
                {
                    foreach (KeyValuePair<string, tAsset> p in temporary[i])
                    {
                        tAsset asset = p.Value;

                        draft.Assets.Add(asset.Id, asset);
                    }
                }
            }

            for (int i = 0; i < temporary.Length; i++)
            {
                if (temporary[i].Count != 0)
                {
                    foreach (KeyValuePair<string, tAsset> p in temporary[i])
                        Expand(0, source, draft, p.Value);
                }
            }

            PullBack(draft, buffer, bufferSize << 1);

            SortedDictionary<int, tAsset> sortedList = [];
            int lastHardcodedId = 0;

            Array.Clear(buffer);

            int newId;

            foreach (KeyValuePair<string, tAsset> p in temporary[0])
            {
                tAsset asset = p.Value;

                Contract.Assert(asset.Id >= bufferSize << 1);

                newId = asset.Id - (bufferSize << 1);

                Contract.Assert(buffer[asset.Id] == 0 && buffer[newId] == 0);

                buffer[asset.Id] = newId;
                buffer[newId] = newId;

                asset.Id = newId;

                sortedList.Add(newId, asset);

                if (lastHardcodedId < newId)
                    lastHardcodedId = newId;
            }

            newId = 0;

            foreach (KeyValuePair<int, tAsset> p in draft.Assets)
            {
                if (buffer[p.Key] != 0)
                    continue;

                tAsset asset = p.Value;

                Contract.Assert(asset.Id == p.Key);

                do
                    newId++;
                while (buffer[newId] != 0);

                Contract.Assert(buffer[asset.Id] == 0 && buffer[newId] == 0);

                buffer[asset.Id] = newId;
                buffer[newId] = newId;

                asset.Id = newId;

                sortedList.Add(newId, asset);
            }

            draft.Clear();

            if (newId < lastHardcodedId)
            {
                while (true)
                {
                    do
                        newId++;
                    while (buffer[newId] != 0);

                    if (newId > lastHardcodedId)
                        break;

                    sortedList.Add(newId, new tGenericRectAsset(newId, string.Empty));
                }
            }

            foreach (KeyValuePair<int, tAsset> p in sortedList)
                draft.Assets.Add(p.Key, p.Value);

            sortedList.Clear();

            Map(draft, buffer, eFunction.Remap);

            source.Assets.Clear();

            foreach (KeyValuePair<int, tAsset> p in draft.Assets)
                source.Assets.Add(p.Key, p.Value);

            draft.Clear();

            // does a final test

            buffer[1] = 1;
            buffer[2] = source.Assets.Count;

            Map(source, buffer, eFunction.Test);

#if DEBUG
            tObject.Parent = source;
#endif

        }

        // expand functions

        private static void Expand(int depth, clsQ3 source, clsQ3 destination, tAsset asset)
        {
            switch (asset.Type)
            {
                case tAsset.eType.Scene:
                    tVisualGroupAsset scene = (tVisualGroupAsset)asset;

                    for (int i = 0; i < scene.Cells.Length; i++)
                        Expand(depth, source, destination, scene.Cells[i].Id);

                    break;

                case tAsset.eType.Button:
                    tPushBtnAsset button = (tPushBtnAsset)asset;

                    for (int i = 0; i < button.SpriteControl.Images.Id.Length; i++)
                        Expand(depth, source, destination, button.SpriteControl.Images.Id[i]);

                    for (int i = 0; i < button.SpriteControl.DisableImages.Id.Length; i++)
                        Expand(depth, source, destination, button.SpriteControl.DisableImages.Id[i]);

                    for (int i = 0; i < button.SpriteControl.HiliteImages.Id.Length; i++)
                        Expand(depth, source, destination, button.SpriteControl.HiliteImages.Id[i]);

                    break;

                case tAsset.eType.StateBtn:
                    tValueBtnAsset stateButton = (tValueBtnAsset)asset;

                    for (int i = 0; i < stateButton.SpriteControl.Images.Id.Length; i++)
                        Expand(depth, source, destination, stateButton.SpriteControl.Images.Id[i]);

                    for (int i = 0; i < stateButton.SpriteControl.DisableImages.Id.Length; i++)
                        Expand(depth, source, destination, stateButton.SpriteControl.DisableImages.Id[i]);

                    for (int i = 0; i < stateButton.SpriteControl.HiliteImages.Id.Length; i++)
                        Expand(depth, source, destination, stateButton.SpriteControl.HiliteImages.Id[i]);

                    break;

                case tAsset.eType.Slider:
                    tSliderAsset slider = (tSliderAsset)asset;

                    Expand(depth, source, destination, slider.ThumbId);

                    break;

                case tAsset.eType.ScrollBox:
                    tScrollBoxAsset scrollBox = (tScrollBoxAsset)asset;

                    if (source.Names.TryGetValue(scrollBox.ChildName.Value, out int id))
                        Expand(depth, source, destination, id);

                    break;

                case tAsset.eType.RadioGroup:
                    tRadioGroupAsset radioGroup = (tRadioGroupAsset)asset;

                    for (int i = 0; i < radioGroup.Cells.Length; i++)
                        Expand(depth, source, destination, radioGroup.Cells[i].Id);

                    break;
            }
        }

        private static void Expand(int depth, clsQ3 source, clsQ3 destination, int id)
        {
            tAsset asset = source.Assets[id];

            destination.Assets.TryAdd(asset.Id, asset);

            if (depth > 2)
                throw new NotSupportedException();

            Expand(depth + 1, source, destination, asset);
        }

        // map functions

        private static void Map(clsQ3 source, int[] buffer, eFunction func)
        {
            source.Update();

            buffer[0] = (int)func;

            foreach (KeyValuePair<int, tAsset> p in source.Assets)
            {
                tAsset asset = p.Value;

                Contract.Assert(p.Value.Id == p.Key);

                Map(source, buffer, asset);
            }
        }

        private static void Map(clsQ3 source, int[] buffer, tAsset asset)
        {
            switch (asset.Type)
            {
                case tAsset.eType.Scene:
                    tVisualGroupAsset scene = (tVisualGroupAsset)asset;

                    for (int i = 0; i < scene.Cells.Length; i++)
                        Map(buffer, ref scene.Cells[i].Id);

                    break;

                case tAsset.eType.Button:
                    tPushBtnAsset button = (tPushBtnAsset)asset;

                    for (int i = 0; i < button.SpriteControl.Images.Id.Length; i++)
                        Map(buffer, ref button.SpriteControl.Images.Id[i]);

                    for (int i = 0; i < button.SpriteControl.DisableImages.Id.Length; i++)
                        Map(buffer, ref button.SpriteControl.DisableImages.Id[i]);

                    for (int i = 0; i < button.SpriteControl.HiliteImages.Id.Length; i++)
                        Map(buffer, ref button.SpriteControl.HiliteImages.Id[i]);

                    break;

                case tAsset.eType.StateBtn:
                    tValueBtnAsset stateButton = (tValueBtnAsset)asset;

                    for (int i = 0; i < stateButton.SpriteControl.Images.Id.Length; i++)
                        Map(buffer, ref stateButton.SpriteControl.Images.Id[i]);

                    for (int i = 0; i < stateButton.SpriteControl.DisableImages.Id.Length; i++)
                        Map(buffer, ref stateButton.SpriteControl.DisableImages.Id[i]);

                    for (int i = 0; i < stateButton.SpriteControl.HiliteImages.Id.Length; i++)
                        Map(buffer, ref stateButton.SpriteControl.HiliteImages.Id[i]);

                    break;

                case tAsset.eType.Slider:
                    tSliderAsset slider = (tSliderAsset)asset;

                    Map(buffer, ref slider.ThumbId);

                    break;

                case tAsset.eType.ScrollBox:
                    tScrollBoxAsset scrollBox = (tScrollBoxAsset)asset;

                    if (source.Names.TryGetValue(scrollBox.ChildName.Value, out int id))
                        Map(buffer, ref id);

                    break;

                case tAsset.eType.RadioGroup:
                    tRadioGroupAsset radioGroup = (tRadioGroupAsset)asset;

                    for (int i = 0; i < radioGroup.Cells.Length; i++)
                        Map(buffer, ref radioGroup.Cells[i].Id);

                    break;
            }
        }

        private static void Map(int[] buffer, ref int id)
        {
            switch (buffer[0])
            {
                case (int)eFunction.Count:
                    Contract.Assert(id > 0);

                    buffer[id]++;

                    break;

                case (int)eFunction.Remap:
                    Contract.Assert(id > 0 && buffer[id] != 0);

                    id = buffer[id];

                    break;

                case (int)eFunction.Test:
                    if (id < buffer[1] || id > buffer[2])
                        throw new NotSupportedException();

                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
