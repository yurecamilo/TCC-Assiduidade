using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class ResumoAulaViewModel : BaseViewModel
    {
        private readonly RelatorioService _relatorioService;
        
        private List<RelatorioAusente> _alunosAusentes;
        private AulaExibicaoDTO _aulaVisualizar;

        public AulaExibicaoDTO AulaVisualizar
        {
            get => _aulaVisualizar;
            set { _aulaVisualizar = value; OnPropertyChanged(); }
        }

        public List<RelatorioAusente> AlunosAusentes
        {
            get => _alunosAusentes;
            set { _alunosAusentes = value; OnPropertyChanged(); }
        }

        
        public ICommand FecharVisualizacaoCommand { get; private set; }

        public ResumoAulaViewModel(AulaExibicaoDTO aula)
        {
            _relatorioService = new RelatorioService();
            AulaVisualizar = aula;
            CarregarDadosAusentes(aula);
        }

        private void CarregarDadosAusentes(AulaExibicaoDTO aula)
        {
            try
            {
                var dados = _relatorioService.ObterDadosRelatorio(aula.AulaId);

                AlunosAusentes = dados ?? new List<RelatorioAusente>();
            }
            catch (Exception ex)
            {
                AlunosAusentes = new List<RelatorioAusente>();
                MessageBox.Show("Erro ao buscar ausentes: " + ex.Message);
            }
        }

        
    }
}