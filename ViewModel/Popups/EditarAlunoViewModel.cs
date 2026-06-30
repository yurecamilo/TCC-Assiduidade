using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class EditarAlunoViewModel : BaseViewModel
    {
        private List<Turma> _listaOriginalDoBanco = new List<Turma>();
        private readonly AlunoService _alunoService;

        private string _matricula;
        private string _nome;
        private string _email;
        private Turma _turmaSelecionada;
        private List<Turma> _turmas;
        private DateTime? _dataEntrada; // Nova propriedade privada para a data

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
            set { 
                _turmaSelecionada = value; 
                OnPropertyChanged(); 
            }
        }

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public DateTime? DataEntrada
        {
            get => _dataEntrada;
            set { _dataEntrada = value; OnPropertyChanged(); }
        }

        public ICommand SalvarAlunoCommand { get; private set; }
        public ICommand FecharJanelaCommand { get; private set; }

        public EditarAlunoViewModel(Action fecharJanela, AlunoExibicaoDTO aluno)
        {
            _alunoService = new AlunoService();
            FecharJanelaCommand = new RelayCommand(fecharJanela);
            SalvarAlunoCommand = new RelayCommand(ExecutarSalvar);

            Matricula = aluno.Matricula;
            Nome = aluno.Nome;
            Email = aluno.Email;
            DataEntrada = aluno.DataEntrada;
            _ = CarregarTurmasDoBanco();
            TurmaSelecionada = _listaOriginalDoBanco.FirstOrDefault(t => t.Nome == aluno.Turma);
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
                MostrarErro("Nao foi possivel carregar as turmas. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }

        private async void ExecutarSalvar()
        {
            try
            {
                var aluno = new Aluno
                {
                    Matricula = Matricula,
                    Nome = Nome,
                    Email = Email,
                    TurmaId = TurmaSelecionada.Id,
                    DataEntrada = DataEntrada 
                };

                _alunoService.Atualizar(aluno);
                await DataCacheService.ForçarAtualizacaoAsync();

                MessageBox.Show("Aluno editado com sucesso!");
                FecharJanelaCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel editar o aluno. Verifique os dados informados e tente novamente.", ex);
            }
        }
    }
}
