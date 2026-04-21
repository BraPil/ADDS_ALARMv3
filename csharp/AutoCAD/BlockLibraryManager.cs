// ADDS Block Library Manager
// Extracted from DrawingManager per ALARMv3 recommendation — single responsibility

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ADDS.AutoCAD
{
    public class BlockLibraryManager
    {
        private readonly string _libraryPath;
        private readonly DrawingManager _drawingMgr;
        private readonly ILogger<BlockLibraryManager> _logger;

        public BlockLibraryManager(
            IConfiguration config,
            DrawingManager drawingMgr,
            ILogger<BlockLibraryManager> logger)
        {
            _libraryPath = config["Blocks:LibraryPath"]
                ?? Environment.GetEnvironmentVariable("ADDS_BLOCK_LIBRARY")
                ?? @"C:\ADDS\Blocks\";
            _drawingMgr = drawingMgr;
            _logger = logger;
        }

        public string[] GetAvailableBlocks() =>
            Directory.GetFiles(_libraryPath, "*.dwg");

        public void LoadBlockLibrary(string category)
        {
            var categoryPath = Path.Combine(_libraryPath, category);
            if (!Directory.Exists(categoryPath))
                throw new DirectoryNotFoundException($"Block category not found: {categoryPath}");

            var files = Directory.GetFiles(categoryPath, "*.dwg");
            _logger.LogInformation("LoadBlockLibrary: loading {Count} blocks from '{Category}'",
                files.Length, category);

            foreach (var file in files)
            {
                var blockName = Path.GetFileNameWithoutExtension(file);
                _drawingMgr.InsertBlock(blockName,
                    new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), 1.0);
            }
        }
    }
}
