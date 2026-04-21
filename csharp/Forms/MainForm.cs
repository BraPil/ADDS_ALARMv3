// ADDS Main Windows Form - .NET Framework 4.5 WinForms
using System;
using System.Data;
using System.Windows.Forms;
using ADDS.DataAccess;
using ADDS.AutoCAD;

namespace ADDS.Forms
{
    public partial class MainForm : Form
    {
        private DrawingManager _drawingMgr;
        private EquipmentRepository _equipRepo;

        public MainForm()
        {
            InitializeComponent();
            _equipRepo = new EquipmentRepository();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                _drawingMgr = new DrawingManager();
                LoadEquipmentGrid();
                statusLabel.Text = "Connected to AutoCAD and Oracle";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "ADDS Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadEquipmentGrid()
        {
            var dt = _equipRepo.GetAllEquipment();
            equipmentGridView.DataSource = dt;
        }

        private void btnDrawPipe_Click(object sender, EventArgs e)
        {
            // Directly calling AutoCAD COM from UI thread
            _drawingMgr.SetLayer("PIPE-STD");
            statusLabel.Text = "Drawing pipe...";
        }

        private void btnEquipReport_Click(object sender, EventArgs e)
        {
            var dt = _equipRepo.GetAllEquipment();
            var reportForm = new ReportForm(dt);
            reportForm.ShowDialog();
        }

        private void btnSyncDB_Click(object sender, EventArgs e)
        {
            // Long-running sync on UI thread - freezes the form
            try
            {
                StoredProcedureRunner.RunProc("ADDS_PKG.SYNC_ALL");
                MessageBox.Show("Database sync complete.");
                LoadEquipmentGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sync failed: {ex.Message}");
            }
        }

        private void btnDeleteEquip_Click(object sender, EventArgs e)
        {
            if (equipmentGridView.SelectedRows.Count == 0) return;
            var tag = equipmentGridView.SelectedRows[0].Cells["TAG"].Value.ToString();
            _equipRepo.DeleteEquipment(tag);
            LoadEquipmentGrid();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OracleConnectionFactory.CloseConnection();
            _drawingMgr?.Release();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Raw SQL filter built from user input
            var filter = txtSearch.Text;
            var dt = StoredProcedureRunner.RunQuery(
                "SELECT * FROM EQUIPMENT WHERE TAG LIKE '%" + filter + "%'");
            equipmentGridView.DataSource = dt;
        }
    }
}
