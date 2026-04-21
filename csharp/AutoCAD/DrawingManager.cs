// ADDS AutoCAD Integration - Drawing Manager
//
// AutoCAD .NET API Compatibility Matrix
// -------------------------------------------------------
// Assembly         | Version Tested | CopyLocal | Notes
// acdbmgd.dll      | 24.1.*.*       | false     | AutoCAD 2021
// acmgd.dll        | 24.1.*.*       | false     | AutoCAD 2021
// AcCoreMgd.dll    | 24.1.*.*       | false     | AutoCAD 2021
// -------------------------------------------------------
// IMPORTANT: All AutoCAD API assemblies MUST have CopyLocal=false in the
// project file. These assemblies are provided by the AutoCAD host process
// and must never be shipped with the plugin. Deploying them alongside the
// plugin will cause version conflicts and silent runtime failures.
//
// Target AutoCAD version: 2021 (internal version 24.1)
// Minimum supported:      2021 (acdbmgd major version 24)
//
// References: Autodesk.AutoCAD.Interop (COM/ActiveX) - legacy, kept for
// compatibility; new code should use the .NET API (AcMgd.dll / acdbmgd.dll).

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;       // COM ActiveX - legacy
using Autodesk.AutoCAD.Interop.Common;

namespace ADDS.AutoCAD
{
    /// <summary>
    /// Performs a fast version check of the AutoCAD host assemblies on first
    /// use.  Call <see cref="VersionGuard.AssertCompatible"/> from any plugin
    /// entry point so that version mismatches surface immediately with a clear,
    /// actionable message instead of a cryptic MissingMethodException or AV.
    /// </summary>
    public static class VersionGuard
    {
        // The major version number embedded in Autodesk assemblies for
        // AutoCAD 2021. Update this constant when the target release changes.
        private const int RequiredAcadMajorVersion = 24;

        // Human-readable product name that corresponds to the version above.
        private const string RequiredAcadProductName = "AutoCAD 2021";

        private static bool _checked;

        /// <summary>
        /// Verifies that the AutoCAD host assemblies loaded in the current
        /// AppDomain match the version this plugin was compiled against.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the major version of acmgd.dll does not match
        /// <see cref="RequiredAcadMajorVersion"/>.  The exception message
        /// includes both the required and the detected version so the user
        /// (or support team) can act on it immediately.
        /// </exception>
        public static void AssertCompatible()
        {
            if (_checked) return;
            _checked = true;

            // Locate acmgd.dll in the currently loaded assemblies.
            Assembly acmgd = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name.Equals("acmgd", StringComparison.OrdinalIgnoreCase))
                {
                    acmgd = asm;
                    break;
                }
            }

            if (acmgd == null)
            {
                throw new InvalidOperationException(
                    "ADDS plugin version check failed: acmgd.dll is not loaded. " +
                    $"This plugin requires {RequiredAcadProductName}. " +
                    "Ensure the plugin is loaded from within a supported AutoCAD process.");
            }

            var loadedVersion = acmgd.GetName().Version;
            if (loadedVersion == null || loadedVersion.Major != RequiredAcadMajorVersion)
            {
                var detected = loadedVersion?.ToString() ?? "<unknown>";
                throw new InvalidOperationException(
                    $"ADDS plugin version mismatch: this plugin requires {RequiredAcadProductName} " +
                    $"(acmgd.dll major version {RequiredAcadMajorVersion}) but the running " +
                    $"AutoCAD host provides acmgd.dll version {detected}. " +
                    "Load the plugin in the correct AutoCAD version or contact your administrator.");
            }
        }
    }

    public class DrawingManager
    {
        private AcadApplication _acadApp;
        private AcadDocument _activeDoc;

        public DrawingManager()
        {
            // Fail fast with a clear message when the host AutoCAD version
            // does not match the version this plugin was compiled against.
            VersionGuard.AssertCompatible();

            // COM interop - fragile across AutoCAD versions
            _acadApp = (AcadApplication)Marshal.GetActiveObject("AutoCAD.Application.24");
            _activeDoc = _acadApp.ActiveDocument;
