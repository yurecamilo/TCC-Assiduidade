using System.Windows;
using System.Windows.Controls;
using TCC_Assiduidade.View.Popups;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para AlunosView.xam
    /// </summary>
    public partial class AlunosView : UserControl
    {
        private readonly AlunosViewModel _viewModel;
        public AlunosView()
        {
            InitializeComponent();
            _viewModel = new AlunosViewModel();
            DataContext = _viewModel;
        }

        public void BtnCadastrarAluno_Click(object sender, RoutedEventArgs e)
        {
                NovoAluno janelaPopUp = new NovoAluno();
    
                janelaPopUp.Owner = Window.GetWindow(this);
                janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    
                janelaPopUp.ShowDialog();
        }
    }
}
