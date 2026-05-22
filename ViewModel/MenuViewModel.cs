using System;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.View;
using TCC_Assiduidade.ViewModel.Base;
using TCC_Assiduidade.ViewModels.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class MenuViewModel : BaseViewModel
    {
        public ICommand AbrirTelaCadastroCommand { get; private set; }
        public ICommand AbrirTelaPresencaCommand { get; private set; }
        public ICommand AbrirTelaRelatoriosAulasCommand { get; private set; }

        public MenuViewModel()
        {
            AbrirTelaCadastroCommand = new RelayCommand(AbrirTelaCadastro);
            AbrirTelaPresencaCommand = new RelayCommand(AbrirTelaPresenca);
            AbrirTelaRelatoriosAulasCommand = new RelayCommand(AbrirTelaRelatoriosAulas);
        }

        private void AbrirTelaCadastro()
        {
            try
            {
                TelaCadastro telaCadastro = new TelaCadastro();
                telaCadastro.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir a tela de cadastro: " + ex.Message);
            }
        }

        private void AbrirTelaPresenca()
        {
            try
            {
                TelaPresenca telaPresenca = new TelaPresenca();
                telaPresenca.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir a tela de presenca: " + ex.Message);
            }
        }

        private void AbrirTelaRelatoriosAulas()
        {
            try
            {
                TelaRelatoriosAulas telaRelatoriosAulas = new TelaRelatoriosAulas();
                telaRelatoriosAulas.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir a tela de relatorios de aulas: " + ex.Message);
            }
        }
    }
}
