// ADDS Main Panel - AutoCAD PaletteSet hosting a WPF UserControl (MVVM)
// Modernized: WinForms replaced with AutoCAD PaletteSet + WPF, async DB calls, no UI-thread blocking

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Microsoft.Extensions.Logging;
using ADDS.DataAccess;
using ADDS.Services;

namespace ADDS.Forms
{
    // ── AutoCAD command that opens the palette ───────────────────────────────

    public static class ADDSPaletteCommand
    {
        private static PaletteSet _paletteSet;

        public static void ShowPalette(ILogger logger, EquipmentRepository equipRepo)
        {
            if (_paletteSet == null)
            {
                _paletteSet = new PaletteSet("ADDS", new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
                _paletteSet.MinimumSize = new System.Drawing.Size(300, 400);

                var vm = new MainPaletteViewModel(logger, equipRepo);
                var panel = new MainPaletteView { DataContext = vm };
                _paletteSet.AddVisual("Equipment", panel);
            }
            _paletteSet.Visible = true;
        }
    }

    // ── ViewModel ────────────────────────────────────────────────────────────

    public class MainPaletteViewModel : INotifyPropertyChanged
    {
        private readonly ILogger _logger;
        private readonly EquipmentRepository _equipRepo;

        public ObservableCollection<EquipmentRow> Equipment { get; } = new();

        private string _status = "Ready";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        public MainPaletteViewModel(ILogger logger, EquipmentRepository equipRepo)
        {
            _logger = logger;
            _equipRepo = equipRepo;
        }

        public async Task LoadAsync()
        {
            Status = "Loading...";
            try
            {
                var dt = await _equipRepo.GetAllEquipmentAsync();
                Equipment.Clear();
                foreach (System.Data.DataRow row in dt.Rows)
                    Equipment.Add(new EquipmentRow(row["TAG"].ToString(), row["TYPE"].ToString(), row["MODEL"].ToString()));
                Status = $"{Equipment.Count} equipment records";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadAsync failed.");
                Status = $"Error: {ex.Message}";
            }
        }

        public async Task DeleteSelectedAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            try
            {
                await _equipRepo.DeleteEquipmentAsync(tag);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAsync failed for {Tag}", tag);
                Status = $"Delete failed: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public record EquipmentRow(string Tag, string Type, string Model);

    // ── WPF View (code-behind minimal by design) ─────────────────────────────

    public partial class MainPaletteView : UserControl
    {
        private MainPaletteViewModel Vm => (MainPaletteViewModel)DataContext;

        public MainPaletteView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await Vm.LoadAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
            await Vm.LoadAsync();

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (equipmentGrid.SelectedItem is EquipmentRow row)
                await Vm.DeleteSelectedAsync(row.Tag);
        }
    }
}
