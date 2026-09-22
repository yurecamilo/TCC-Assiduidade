using System.Windows.Controls;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Dashboards;

namespace TCC_Assiduidade.View.Dashboards
{
    public partial class DashboardTurmasView : UserControl
    {
        public DashboardTurmasView(DashboardsViewModel pai, DashboardService dashboardService)
        {
            InitializeComponent();
            DataContext = new DashboardTurmasViewModel(pai, dashboardService);
        }
    }
}
