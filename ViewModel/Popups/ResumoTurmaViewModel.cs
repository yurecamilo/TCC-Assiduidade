using System.Collections.Generic;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class ResumoTurmaViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;
        private readonly AlunoService _alunoService;

        private TurmaExibicaoDTO _turmaInfo;
        private List<Aluno> _alunosDaTurma;

        public TurmaExibicaoDTO TurmaInfo
        {
            get => _turmaInfo;
            set { _turmaInfo = value; OnPropertyChanged(); }
        }

        public List<Aluno> AlunosDaTurma
        {
            get => _alunosDaTurma;
            set { _alunosDaTurma = value; OnPropertyChanged(); }
        }

        // Construtor recebe a turma selecionada para carregar os dados
        public ResumoTurmaViewModel(TurmaExibicaoDTO turmaSelecionada)
        {
            _turmaService = new TurmaService();
            _alunoService = new AlunoService();
            TurmaInfo = turmaSelecionada;
            CarregarAlunosDaTurma();
        }

        private void CarregarAlunosDaTurma()
        {
            var turma = _turmaService.ObterTurmaPorNome(TurmaInfo.Nome);
            AlunosDaTurma = _alunoService.ObterPorTurma(turma.Id);
        }
    }
}