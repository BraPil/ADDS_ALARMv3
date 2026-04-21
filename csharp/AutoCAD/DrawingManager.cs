// ADDS AutoCAD Integration - Drawing Manager
// Modernized: COM/ActiveX replaced with AutoCAD .NET API (AcMgd.dll / AcCoreMgd.dll)
// BlockLibraryManager extracted to its own class file

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Microsoft.Extensions.Logging;

namespace ADDS.AutoCAD
{
    public class DrawingManager
    {
        private readonly ILogger<DrawingManager> _logger;

        public DrawingManager(ILogger<DrawingManager> logger)
        {
            _logger = logger;
        }

        private static Document ActiveDoc =>
            Application.DocumentManager.MdiActiveDocument;

        public void DrawLine(Point3d start, Point3d end, string layer)
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var btr = GetModelSpace(tr, OpenMode.ForWrite);
            var line = new Line(start, end) { Layer = layer };
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
            tr.Commit();
            _logger.LogDebug("DrawLine on layer {Layer}", layer);
        }

        public void DrawCircle(Point3d center, double radius, string layer)
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var btr = GetModelSpace(tr, OpenMode.ForWrite);
            var circle = new Circle(center, Vector3d.ZAxis, radius) { Layer = layer };
            btr.AppendEntity(circle);
            tr.AddNewlyCreatedDBObject(circle, true);
            tr.Commit();
        }

        public void InsertBlock(string blockName, Point3d insertPoint, double scale)
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var db = ActiveDoc.Database;
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (!bt.Has(blockName))
                throw new ArgumentException($"Block '{blockName}' not found in drawing.");

            var btr = GetModelSpace(tr, OpenMode.ForWrite);
            var blkRef = new BlockReference(insertPoint, bt[blockName]);
            blkRef.ScaleFactors = new Scale3d(scale);
            btr.AppendEntity(blkRef);
            tr.AddNewlyCreatedDBObject(blkRef, true);
            tr.Commit();
            _logger.LogDebug("InsertBlock: {Block} at {Point}", blockName, insertPoint);
        }

        public void AddText(Point3d position, string text, double height, string layer)
        {
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var btr = GetModelSpace(tr, OpenMode.ForWrite);
            var mtext = new MText
            {
                Contents  = text,
                Location  = position,
                TextHeight = height,
                Layer     = layer,
            };
            btr.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
            tr.Commit();
        }

        public void SaveDrawing() => ActiveDoc.Database.Save();

        public void SaveDrawingAs(string path) =>
            ActiveDoc.Database.SaveAs(path, DwgVersion.Current);

        public IReadOnlyList<string> GetAllLayerNames()
        {
            var names = new List<string>();
            using var tr = ActiveDoc.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(
                ActiveDoc.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                names.Add(layer.Name);
            }
            tr.Commit();
            return names;
        }

        public void ZoomExtents() =>
            ActiveDoc.Editor.Command("_ZOOM", "_E");

        private static BlockTableRecord GetModelSpace(Transaction tr, OpenMode mode)
        {
            var db = ActiveDoc.Database;
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], mode);
        }
    }
}
