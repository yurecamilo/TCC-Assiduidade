using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.Modelos.DTO
{
    public class TurmaExibicaoDTO: BaseViewModel
    {
        public Turma TurmaOriginal { get; set; }
        public string Nome { get; set; }
        public int QuantidadeAlunos { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
        public bool IsAtiva => TurmaOriginal != null && TurmaOriginal.Status == EStatusTurma.Ativa;
        public bool IsArquivada => TurmaOriginal != null && TurmaOriginal.Status == EStatusTurma.Arquivada;
        public bool PodeExcluir => TurmaOriginal != null && TurmaOriginal.Status != EStatusTurma.Excluida;
    }
}
