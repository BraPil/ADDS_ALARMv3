// ADDS AutoCAD Integration - Drawing Manager
// References: Autodesk.AutoCAD.Interop (COM/ActiveX) - AutoCAD 2000-era
// TODO: Migrate to AutoCAD .NET API (AcMgd.dll)

using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;       // COM ActiveX - legacy
            _activeDoc = _acadApp.ActiveDocument;
        }

        /// <summary>
        /// Draws a line on the specified layer.
        /// Transaction-safe: uses StartUndoMark/EndUndoMark to guarantee the undo
        /// group is always closed even if a COM exception is thrown.
        /// NOTE: COM interop does not support Autodesk.AutoCAD.DatabaseServices.Transaction;
        /// StartUndoMark/EndUndoMark is the COM equivalent for undo grouping.
        /// Migrate to AcMgd.dll Transaction when .NET API migration is complete (see TODO above).
        /// </summary>
        public void DrawLine(double[] start, double[] end, string layer)
        {
            _activeDoc.StartUndoMark();
            try
            {
                SetLayer(layer);
                var modelSpace = _activeDoc.ModelSpace;
                modelSpace.AddLine(start, end);
            }
            finally
            {
                _activeDoc.EndUndoMark();
            }
        }

        /// <summary>
        /// Draws a circle on the specified layer.
        /// Transaction-safe: uses StartUndoMark/EndUndoMark to guarantee the undo
        /// group is always closed even if a COM exception is thrown.
        /// NOTE: COM interop does not support Autodesk.AutoCAD.DatabaseServices.Transaction;
        /// StartUndoMark/EndUndoMark is the COM equivalent for undo grouping.
        /// Migrate to AcMgd.dll Transaction when .NET API migration is complete (see TODO above).
        /// </summary>
        public void DrawCircle(double[] center, double radius, string layer)
        {
            _activeDoc.StartUndoMark();
            try
            {
                SetLayer(layer);
                var modelSpace = _activeDoc.ModelSpace;
                modelSpace.AddCircle(center, radius);
            }
            finally
            {
                _activeDoc.EndUndoMark();
            }
        }

        public void InsertBlock(string blockName, double[] insertPoint, double scale)
            }
        }

        /// <summary>
        /// Adds text to model space at the given position.
        /// Transaction-safe: uses StartUndoMark/EndUndoMark to guarantee the undo
        /// group is always closed even if a COM exception is thrown.
        /// NOTE: COM interop does not support Autodesk.AutoCAD.DatabaseServices.Transaction;
        /// StartUndoMark/EndUndoMark is the COM equivalent for undo grouping.
        /// Migrate to AcMgd.dll Transaction when .NET API migration is complete (see TODO above).
        /// </summary>
        public void AddText(double[] position, string text, double height)
        {
            _activeDoc.StartUndoMark();
            try
            {
                var modelSpace = _activeDoc.ModelSpace;
                modelSpace.AddText(text, position, height);
            }
            finally
            {
                _activeDoc.EndUndoMark();
            }
        }

        public void SaveDrawing()
