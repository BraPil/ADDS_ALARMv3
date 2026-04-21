// ADDS Layer Management - AutoCAD COM interop
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;

            }
        }

        /// <summary>
        /// Freezes all layers that do not start with "ADDS-" and are not the "0" layer.
        /// Transaction-safe: uses StartUndoMark/EndUndoMark on the document to guarantee
        /// the undo group is always closed even if a COM exception is thrown on a specific layer.
        /// Each layer freeze is individually guarded to allow processing to continue if one
        /// layer (e.g. the current active layer) throws a COM exception when frozen.
        /// NOTE: COM interop does not support Autodesk.AutoCAD.DatabaseServices.Transaction;
        /// StartUndoMark/EndUndoMark is the COM equivalent for undo grouping.
        /// Migrate to AcMgd.dll Transaction when .NET API migration is complete (see TODO in DrawingManager.cs).
        /// </summary>
        public static void FreezeNonADDSLayers(AcadDocument doc)
        {
            doc.StartUndoMark();
            try
            {
                foreach (AcadLayer l in doc.Layers)
                {
                    if (!l.Name.StartsWith("ADDS-") && l.Name != "0")
                    {
                        try
                        {
                            l.Freeze = true;
                        }
                        catch (COMException)
                        {
                            // Freezing the current active layer throws a COM exception;
                            // skip that layer and continue processing remaining layers.
                        }
                    }
                }
            }
            finally
            {
                doc.EndUndoMark();
            }
        }
