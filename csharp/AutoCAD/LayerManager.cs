// ADDS Layer Management - AutoCAD COM interop
//
// AutoCAD .NET API Compatibility Matrix
// -------------------------------------------------------
// Assembly         | Version Tested | CopyLocal | Notes
// acdbmgd.dll      | 24.1.*.*       | false     | AutoCAD 2021
// acmgd.dll        | 24.1.*.*       | false     | AutoCAD 2021
// AcCoreMgd.dll    | 24.1.*.*       | false     | AutoCAD 2021
// -------------------------------------------------------
// All AutoCAD API assemblies MUST have CopyLocal=false.
// See DrawingManager.cs for the full compatibility policy.
//
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Interop;
            {"ADDS-ANNOTATION", 1}, {"ADDS-DIMENSION", 8}
        };

        /// <summary>
        /// Initialises standard ADDS layers via <see cref="DrawingManager"/>.
        /// Performs a host-version check before any AutoCAD API call so that
        /// version mismatches fail fast with an actionable error message.
        /// </summary>
        public static void SetupStandardLayers(DrawingManager dm)
        {
            // Guard is idempotent; calling it here ensures every public entry
            // point in this class validates the host version before use.
            VersionGuard.AssertCompatible();

            foreach (var layer in LayerColors.Keys)
            {
                dm.SetLayer(layer);
