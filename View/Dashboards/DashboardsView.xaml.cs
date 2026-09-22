using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View.Dashboards
{
    public partial class DashboardsView : UserControl
    {
        public DashboardsView()
        {
            InitializeComponent();
            DataContext = new DashboardsViewModel();
        }
    }
}
