// ADDS Layer Management
// Modernized: COM interop replaced with AutoCAD .NET API
// AutoCAD .NET API Compatibility Matrix
// -------------------------------------------------------
// Assembly                  | Version Tested | Notes
// AcMgd.dll                 | 24.x (2024)    | Application, Document
// AcCoreMgd.dll             | 24.x (2024)    | Core DB operations
// AcDbMgd.dll               | 24.x (2024)    | LayerTable, BlockTable, etc.
// -------------------------------------------------------

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.Extensions.Logging;

namespace ADDS.AutoCAD
{
    public class LayerManager
    {
        private static readonly Dictionary<string, short> LayerColors = new()
        {
            {"PIPE-STD",         7},
            {"PIPE-INSULATED",   5},
            {"VESSEL",           3},
            {"INSTRUMENT",       4},
            {"STRUCTURAL",       6},
            {"ELECTRICAL",       2},
            {"ADDS-ANNOTATION",  1},
            {"ADDS-DIMENSION",   8},
        };

        private readonly ILogger<LayerManager> _logger;

        public LayerManager(ILogger<LayerManager> logger)
        {
            _logger = logger;
        }

        private static Document ActiveDoc =>
            Application.DocumentManager.MdiActiveDocument;

        public void SetupStandardLayers()
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var db = ActiveDoc.Database;
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

            foreach (var (name, colorIndex) in LayerColors)
            {
                if (!lt.Has(name))
                {
                    var ltr = new LayerTableRecord
                    {
                        Name  = name,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex),
                    };
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
            }
            tr.Commit();
            _logger.LogInformation("SetupStandardLayers: {Count} layers configured.", LayerColors.Count);
        }

        public void FreezeNonADDSLayers()
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var db = ActiveDoc.Database;
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            int count = 0;

            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite);
                if (!ltr.Name.StartsWith("ADDS-", StringComparison.OrdinalIgnoreCase)
                    && ltr.Name != "0")
                {
                    ltr.IsFrozen = true;
                    count++;
                }
            }
            tr.Commit();
            _logger.LogInformation("FreezeNonADDSLayers: froze {Count} layers.", count);
        }

        public void ThawAllLayers()
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var db = ActiveDoc.Database;
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite);
                ltr.IsFrozen = false;
            }
            tr.Commit();
        }

        public void PurgeUnusedLayers()
        {
            var db = ActiveDoc.Database;
            var ids = new ObjectIdCollection();
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
                ids.Add(id);
            tr.Commit();

            db.Purge(ids);
            using var tr2 = ActiveDoc.TransactionManager.StartTransaction();
            foreach (ObjectId id in ids)
            {
                var obj = tr2.GetObject(id, OpenMode.ForWrite, false, true);
                obj.Erase();
            }
            tr2.Commit();
            _logger.LogInformation("PurgeUnusedLayers: removed {Count} layers.", ids.Count);
        }

        public IReadOnlyList<string> GetLayersByPrefix(string prefix)
        {
            var result = new List<string>();
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var db = ActiveDoc.Database;
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (ltr.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    result.Add(ltr.Name);
            }
            tr.Commit();
            return result;
        }
    }
}
