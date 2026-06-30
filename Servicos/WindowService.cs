using System;
using System.Linq;
using System.Windows;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.View.Popups;

namespace TCC_Assiduidade.Servicos
{
    public static class WindowService
    {
        private static void ConfigurarEAbrir(Window janela)
        {
            Window activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            janela.Owner = activeWindow;
            janela.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            janela.ShowDialog();
        }

        public static void AbrirImportarCadastro() => ConfigurarEAbrir(new ImportarCadastro());
        public static void AbrirImportarPresenca() => ConfigurarEAbrir(new ImportarPresenca());
        public static void AbrirNovaTurma() => ConfigurarEAbrir(new NovaTurma());
        public static void AbrirNovoAluno() => ConfigurarEAbrir(new NovoAluno());
        public static void AbrirEditarTurma(TurmaExibicaoDTO turma) => ConfigurarEAbrir(new EditarTurma(turma));
        public static void AbrirVisualizarTurma(TurmaExibicaoDTO turma) => ConfigurarEAbrir(new ResumoTurma(turma));
        public static void AbrirEditarAluno(AlunoExibicaoDTO aluno) => ConfigurarEAbrir(new EditarAluno(aluno));
    }
}
