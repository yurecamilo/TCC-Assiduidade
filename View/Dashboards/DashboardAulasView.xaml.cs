using System.Windows.Controls;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Dashboards;

namespace TCC_Assiduidade.View.Dashboards
{
    public partial class DashboardAulasView : UserControl
    {
        public DashboardAulasView(DashboardsViewModel pai, DashboardService dashboardService)
        {
            InitializeComponent();
            DataContext = new DashboardAulasViewModel(pai, dashboardService);
        }
    }
}
