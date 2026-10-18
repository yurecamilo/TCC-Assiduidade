using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
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
            TurmaInfo = turmaSelecionada ?? throw new ArgumentNullException(nameof(turmaSelecionada));
            CarregarAlunosDaTurma();
        }

        private void CarregarAlunosDaTurma()
        {
            // O nome de exibição recebe o sufixo "(arquivada)"; a consulta deve
            // usar o nome persistido da turma, preservado em TurmaOriginal.
            string nomePersistido = TurmaInfo?.TurmaOriginal?.Nome;
            if (string.IsNullOrWhiteSpace(nomePersistido))
            {
                AlunosDaTurma = new List<Aluno>();
                return;
            }

            var turma = _turmaService.ObterTurmaPorNome(nomePersistido);
            AlunosDaTurma = turma == null
                ? new List<Aluno>()
                : _alunoService.ObterPorTurma(turma.Id) ?? new List<Aluno>();
        }
    }
}