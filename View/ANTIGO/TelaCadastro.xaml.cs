using System.Configuration;
using System.Windows;
using TCC_Assiduidade.ViewModel.ANTIGO;

namespace TCC_Assiduidade.View.ANTIGO
{
    public partial class TelaCadastro : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public TelaCadastro()
        {
            InitializeComponent();
            this.DataContext = new CadastroViewModel();
        }
    }
}
