// ADDS AutoCAD Integration - Drawing Manager
// References: Autodesk.AutoCAD.Interop (COM/ActiveX) - AutoCAD 2000-era
// TODO: Migrate to AutoCAD .NET API (AcMgd.dll)

using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;       // COM ActiveX - legacy
using Autodesk.AutoCAD.Interop.Common;

namespace ADDS.AutoCAD
{
    public class DrawingManager
    {
        private AcadApplication _acadApp;
        private AcadDocument _activeDoc;

        public DrawingManager()
        {
            // COM interop - fragile across AutoCAD versions
            _acadApp = (AcadApplication)Marshal.GetActiveObject("AutoCAD.Application.24");
            _activeDoc = _acadApp.ActiveDocument;
        }

        public void DrawLine(double[] start, double[] end, string layer)
        {
            SetLayer(layer);
            var modelSpace = _activeDoc.ModelSpace;
            modelSpace.AddLine(start, end);
        }

        public void DrawCircle(double[] center, double radius, string layer)
        {
            SetLayer(layer);
            var modelSpace = _activeDoc.ModelSpace;
            modelSpace.AddCircle(center, radius);
        }

        public void InsertBlock(string blockName, double[] insertPoint, double scale)
        {
            var modelSpace = _activeDoc.ModelSpace;
            modelSpace.InsertBlock(insertPoint, blockName, scale, scale, scale, 0);
        }

        public void SetLayer(string layerName)
        {
            try
            {
                var layers = _activeDoc.Layers;
                AcadLayer layer = null;
                foreach (AcadLayer l in layers)
                {
                    if (l.Name == layerName) { layer = l; break; }
                }
                if (layer == null)
                    layer = layers.Add(layerName);
                _activeDoc.ActiveLayer = layer;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Layer operation failed: {ex.Message}");
            }
        }

        public void AddText(double[] position, string text, double height)
        {
            var modelSpace = _activeDoc.ModelSpace;
            modelSpace.AddText(text, position, height);
        }

        public void SaveDrawing()
        {
            _activeDoc.Save();
        }

        public void SaveDrawingAs(string path)
        {
            // AcSaveAsType enum - version-specific
            _activeDoc.SaveAs(path, AcSaveAsType.acR2000_dxf);
        }

        public string[] GetAllLayerNames()
        {
            var layers = _activeDoc.Layers;
            var names = new string[layers.Count];
            int i = 0;
            foreach (AcadLayer l in layers) { names[i++] = l.Name; }
            return names;
        }

        public void ZoomExtents()
        {
            _acadApp.ZoomExtents();
        }

        public void Release()
        {
            if (_activeDoc != null) Marshal.ReleaseComObject(_activeDoc);
            if (_acadApp != null) Marshal.ReleaseComObject(_acadApp);
        }
    }

    public class BlockLibraryManager
    {
        private const string BLOCK_LIBRARY_PATH = @"C:\ADDS\Blocks\";

        public static string[] GetAvailableBlocks()
        {
            return System.IO.Directory.GetFiles(BLOCK_LIBRARY_PATH, "*.dwg");
        }

        public static void LoadBlockLibrary(DrawingManager dm, string category)
        {
            var blockPath = BLOCK_LIBRARY_PATH + category;
            var files = System.IO.Directory.GetFiles(blockPath, "*.dwg");
            // Load all blocks into drawing - expensive operation
            foreach (var f in files)
            {
                dm.InsertBlock(f, new double[] { 0, 0, 0 }, 1.0);
            }
        }
    }
}
