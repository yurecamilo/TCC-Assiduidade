using System.Windows.Input;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.View.Dashboards;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class DashboardsViewModel : BaseViewModel
    {
        private readonly DashboardService _dashboardService;
        private object _conteudoAtual;

        public DashboardsViewModel()
        {
            _dashboardService = new DashboardService();
            AbrirDashboardAlunosCommand = new RelayCommand(AbrirDashboardAlunos);
            AbrirDashboardAulasCommand = new RelayCommand(AbrirDashboardAulas);
            AbrirDashboardTurmasCommand = new RelayCommand(AbrirDashboardTurmas);
        }

        public object ConteudoAtual
        {
            get => _conteudoAtual;
            set
            {
                _conteudoAtual = value;
                OnPropertyChanged();
            }
        }

        public ICommand AbrirDashboardAlunosCommand { get; }
        public ICommand AbrirDashboardAulasCommand { get; }
        public ICommand AbrirDashboardTurmasCommand { get; }

        private void AbrirDashboardAlunos()
        {
            ConteudoAtual = new DashboardAlunosView(this, _dashboardService);
        }

        private void AbrirDashboardAulas()
        {
            ConteudoAtual = new DashboardAulasView(this, _dashboardService);
        }

        private void AbrirDashboardTurmas()
        {
            ConteudoAtual = new DashboardTurmasView(this, _dashboardService);
        }
    }
}
