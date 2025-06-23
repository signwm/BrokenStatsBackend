using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrokenStatsFrontendWinForms.Services;

namespace BrokenStatsFrontendWinForms
{
    public partial class MainForm : Form
    {
        private readonly IBackendService _backend;

        public MainForm(IBackendService backend)
        {
            _backend = backend;
            InitializeComponent();
        }

        private async void loadButton_Click(object sender, EventArgs e)
        {
            await LoadInstancesAsync();
        }

        private async Task LoadInstancesAsync()
        {
            var from = startPicker.Value;
            var to = endPicker.Value;
            try
            {
                var list = await _backend.GetInstancesAsync(from, to);
                instancesGrid.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private async void instancesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (instancesGrid.CurrentRow?.DataBoundItem is InstanceDto inst)
            {
                try
                {
                    var list = await _backend.GetFightsAsync(inst.id);
                    fightsGrid.DataSource = list;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }
    }

}
