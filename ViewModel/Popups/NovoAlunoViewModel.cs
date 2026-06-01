using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class NovoAlunoViewModel : BaseViewModel
    {
        private List<Turma> _listaOriginalDoBanco = new List<Turma>();
        private readonly AlunoService _alunoService;

        private string _matricula;
        private string _nome;
        private string _email;
        private Turma _turmaSelecionada;
        private List<Turma> _turmas;

        public string Matricula
        {
            get => _matricula;
            set { _matricula = value; OnPropertyChanged(); }
        }

        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public Turma TurmaSelecionada
        {
            get => _turmaSelecionada;
            set { _turmaSelecionada = value; OnPropertyChanged(); }
        }

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public ICommand SalvarAlunoCommand { get; private set; }
        private readonly Action _fecharJanela;

        public NovoAlunoViewModel(Action fecharJanela)
        {
            _alunoService = new AlunoService();
            _fecharJanela = fecharJanela;
            SalvarAlunoCommand = new RelayCommand(ExecutarSalvar);

            // Carrega as turmas do cache para o ComboBox
            _ = CarregarTurmasDoBanco();
        }

        async Task CarregarTurmasDoBanco()
        {
            try
            {
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.TurmaModeloCache ?? new List<Turma>();
                Turmas = _listaOriginalDoBanco;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar turmas do cache: " + ex.Message);
            }
        }

        private async void ExecutarSalvar()
        {
            if (string.IsNullOrWhiteSpace(Matricula) || string.IsNullOrWhiteSpace(Nome) || TurmaSelecionada == null)
            {
                MessageBox.Show("Por favor, preencha todos os campos obrigatórios (Matrícula, Nome e Turma).");
                return;
            }

            try
            {
                var aluno = new Aluno
                {
                    Matricula = Matricula,
                    Nome = Nome,
                    Email = Email,
                    TurmaId = TurmaSelecionada.Id
                };
                _alunoService.Adicionar(aluno);
                await DataCacheService.ForçarAtualizacaoAsync();

                MessageBox.Show("Aluno cadastrado com sucesso!");
                _fecharJanela?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao cadastrar aluno: " + ex.Message);
            }
        }
    }
}