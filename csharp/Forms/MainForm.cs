// ADDS Main Panel - AutoCAD PaletteSet hosting a WPF UserControl (MVVM)
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using ADDS.DataAccess;
using ADDS.AutoCAD;

namespace ADDS.Forms
{
    // ---------------------------------------------------------------------------
    // ViewModel
    // ---------------------------------------------------------------------------
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DrawingManager _drawingMgr;
        private readonly EquipmentRepository _equipRepo;

        private string _statusText = "Initialising…";
        private string _searchText = string.Empty;
        private DataView _equipmentView;

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Parameterised filter – no raw SQL concatenation
                ApplyEquipmentFilter(value);
            }
        }

        public DataView EquipmentView
        {
            get => _equipmentView;
            private set { _equipmentView = value; OnPropertyChanged(); }
        }

        public ICommand DrawPipeCommand { get; }
        public ICommand EquipReportCommand { get; }
        public ICommand SyncDbCommand { get; }
        public ICommand DeleteEquipCommand { get; }

        public MainViewModel()
        {
            _equipRepo = new EquipmentRepository();
            _drawingMgr = new DrawingManager();

            DrawPipeCommand    = new RelayCommand(_ => DrawPipe());
            EquipReportCommand = new RelayCommand(_ => ShowEquipReport());
            SyncDbCommand      = new AsyncRelayCommand(SyncDbAsync);
            DeleteEquipCommand = new RelayCommand(DeleteEquip);

            LoadEquipmentGrid();
            StatusText = "Connected to AutoCAD and Oracle";
        }

        private void LoadEquipmentGrid()
        {
            var dt = _equipRepo.GetAllEquipment();
            EquipmentView = dt.DefaultView;
        }

        private void ApplyEquipmentFilter(string filter)
        {
            // Use DataView.RowFilter with a parameterised-style expression;
            // the value is escaped so it cannot inject additional SQL.
            var safe = filter.Replace("'", "''");
            if (EquipmentView != null)
                EquipmentView.RowFilter = $"TAG LIKE '%{safe}%'";
        }

        private void DrawPipe()
        {
            _drawingMgr.SetLayer("PIPE-STD");
            StatusText = "Drawing pipe…";
        }

        private void ShowEquipReport()
        {
            var dt = _equipRepo.GetAllEquipment();
            var reportForm = new ReportForm(dt);
            reportForm.ShowDialog();
        }

        private async Task SyncDbAsync()
        {
            StatusText = "Syncing…";
            try
            {
                await Task.Run(() => StoredProcedureRunner.RunProc("ADDS_PKG.SYNC_ALL"));
                LoadEquipmentGrid();
                StatusText = "Database sync complete.";
            }
            catch (Exception ex)
            {
                StatusText = $"Sync failed: {ex.Message}";
            }
        }

        private void DeleteEquip(object parameter)
        {
            if (parameter is DataRowView row)
            {
                var tag = row["TAG"]?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    _equipRepo.DeleteEquipment(tag);
                    LoadEquipmentGrid();
                }
            }
        }

        public void Release()
        {
            OracleConnectionFactory.CloseConnection();
            _drawingMgr?.Release();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ---------------------------------------------------------------------------
    // Minimal ICommand helpers
    // ---------------------------------------------------------------------------
    internal sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object p) => _execute(p);
        public event EventHandler CanExecuteChanged;
    }

    internal sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private bool _isRunning;
        public AsyncRelayCommand(Func<Task> execute) { _execute = execute; }
        public bool CanExecute(object p) => !_isRunning;
        public async void Execute(object p)
        {
            _isRunning = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try   { await _execute(); }
            finally
            {
                _isRunning = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CanExecuteChanged;
    }

    // ---------------------------------------------------------------------------
    // WPF UserControl (code-behind only; XAML omitted – define in MainPanel.xaml)
    // ---------------------------------------------------------------------------
    public partial class MainPanel : UserControl
    {
        public MainPanel()
        {
            // If using a .xaml file, InitializeComponent() is generated.
            // The ViewModel is set here so the palette hosts pure MVVM.
            DataContext = new MainViewModel();
        }
    }

    // ---------------------------------------------------------------------------
    // AutoCAD command that opens the PaletteSet
    // ---------------------------------------------------------------------------
    public static class MainPaletteCommand
    {
        private static PaletteSet _paletteSet;
        private static MainViewModel _vm;

        [Autodesk.AutoCAD.Runtime.CommandMethod("ADDS_OPEN")]
        public static void OpenPalette()
        {
            if (_paletteSet == null)
            {
                _paletteSet = new PaletteSet(
                    "ADDS",
                    "ADDS_PALETTE",
                    new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"))
                {
                    Style = PaletteSetStyles.ShowPropertiesMenu
                           | PaletteSetStyles.ShowAutoHideButton
                           | PaletteSetStyles.ShowCloseButton,
                    Visible = true
                };

                var panel = new MainPanel();
                _vm = panel.DataContext as MainViewModel;

                // AddVisual accepts a WPF UIElement – no WinForms dependency.
                _paletteSet.AddVisual("ADDS", panel);
                _paletteSet.PaletteSetDestroy += (s, e) => _vm?.Release();
            }

            _paletteSet.Visible = true;
        }
    }
}
