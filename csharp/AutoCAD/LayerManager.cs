// ADDS Layer Management - AutoCAD COM interop
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;

namespace ADDS.AutoCAD
{
    public class LayerManager
    {
        private static readonly Dictionary<string, int> LayerColors = new Dictionary<string, int>
        {
            {"PIPE-STD", 7}, {"PIPE-INSULATED", 5}, {"VESSEL", 3},
            {"INSTRUMENT", 4}, {"STRUCTURAL", 6}, {"ELECTRICAL", 2},
            {"ADDS-ANNOTATION", 1}, {"ADDS-DIMENSION", 8}
        };

        public static void SetupStandardLayers(DrawingManager dm)
        {
            foreach (var layer in LayerColors.Keys)
            {
                dm.SetLayer(layer);
            }
        }

        public static void FreezeNonADDSLayers(AcadDocument doc)
        {
            foreach (AcadLayer l in doc.Layers)
            {
                if (!l.Name.StartsWith("ADDS-") && l.Name != "0")
                    l.Freeze = true;
            }
        }

        public static void ThawAllLayers(AcadDocument doc)
        {
            foreach (AcadLayer l in doc.Layers)
                l.Freeze = false;
        }

        public static void PurgeUnusedLayers(AcadDocument doc)
        {
            // COM purge - version-dependent behavior
            doc.PurgeAll();
        }

        public static List<string> GetLayersByPrefix(AcadDocument doc, string prefix)
        {
            var result = new List<string>();
            foreach (AcadLayer l in doc.Layers)
            {
                if (l.Name.StartsWith(prefix))
                    result.Add(l.Name);
            }
            return result;
        }
    }
}
