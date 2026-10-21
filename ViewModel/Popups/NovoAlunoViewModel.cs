using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
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
            set { _turmaSelecionada = value; OnPropertyChanged(); }
        }

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        // NOVA PROPRIEDADE PÚBLICA: Vinculada diretamente com o SelectedDate do DatePicker no XAML
        public DateTime? DataEntrada
        {
            get => _dataEntrada;
            set { _dataEntrada = value; OnPropertyChanged(); }
        }

        public ICommand SalvarAlunoCommand { get; private set; }
        public ICommand FecharJanelaCommand { get; private set; }

        public NovoAlunoViewModel(Action fecharJanela)
        {
            _alunoService = new AlunoService();
            FecharJanelaCommand = new RelayCommand(() => fecharJanela?.Invoke());
            SalvarAlunoCommand = new RelayCommand(ExecutarSalvar);

            // Define a data padrão exibida na tela como o dia de hoje ao abrir a janela
            DataEntrada = DateTime.Now.Date;

            // Carrega as turmas do cache para o ComboBox
            _ = CarregarTurmasDoBanco();
        }

        async Task CarregarTurmasDoBanco()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

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
            if (string.IsNullOrWhiteSpace(Matricula) || string.IsNullOrWhiteSpace(Nome) || TurmaSelecionada == null)
            {
                PopupMensagem.Show("Por favor, preencha todos os campos obrigatórios (Matrícula, Nome e Turma).");
                return;
            }

            try
            {
                var aluno = new Aluno
                {
                    Matricula = Matricula,
                    Nome = Nome,
                    Email = Email,
                    TurmaId = TurmaSelecionada.Id,
                    DataEntrada = DataEntrada // Repassa a propriedade preenchida do DatePicker
                };

                _alunoService.Adicionar(aluno);

                // A gravação no banco já aconteceu; uma falha ao atualizar o cache não
                // deve ser apresentada como erro de salvamento (o usuário tentaria
                // salvar de novo e tomaria "matrícula duplicada").
                string avisoAtualizacao = null;
                try
                {
                    await DataCacheService.ForçarAtualizacaoAsync();
                }
                catch (Exception)
                {
                    avisoAtualizacao = " O aluno foi gravado, mas a lista não conseguiu atualizar. Reabra a tela de alunos para ver os dados atualizados.";
                }

                PopupMensagem.Show(
                    "Aluno cadastrado com sucesso!" + avisoAtualizacao,
                    "Aluno salvo",
                    MessageBoxButton.OK,
                    avisoAtualizacao != null ? MessageBoxImage.Warning : MessageBoxImage.Information);
                FecharJanelaCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel cadastrar o aluno. Verifique os dados informados e tente novamente.", ex);
            }
        }
    }
}
