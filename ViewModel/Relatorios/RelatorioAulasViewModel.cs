using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class RelatorioAulasViewModel : BaseViewModel
    {
        private List<AulaExibicaoDTO> _listaOriginalDoBanco = new List<AulaExibicaoDTO>();
        private readonly RelatorioService _relatorioService;

        private IEnumerable<TurmaExibicaoDTO> _turmas;
        private TurmaExibicaoDTO _turmaSelecionada;
        private IEnumerable<AulaExibicaoDTO> _aulas;
        private AulaExibicaoDTO _aulaSelecionada;
        private DateTime? _dataInicio;
        private DateTime? _dataFim;
        private List<RelatorioAusente> _alunosAusentes;

        public List<RelatorioAusente> AlunosAusentes
        {
            get => _alunosAusentes;
            set { _alunosAusentes = value; OnPropertyChanged(); }
        }
        public IEnumerable<TurmaExibicaoDTO> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public TurmaExibicaoDTO TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                _turmaSelecionada = value;
                OnPropertyChanged();
                ExecutarBusca(); // Busca automática quando muda a turma
            }
        }

        public IEnumerable<AulaExibicaoDTO> Aulas
        {
            get => _aulas;
            set { _aulas = value; OnPropertyChanged(); }
        }

        public AulaExibicaoDTO AulaSelecionada
        {
            get => _aulaSelecionada;
            set { _aulaSelecionada = value; OnPropertyChanged(); }
        }

        public DateTime? DataInicio
        {
            get => _dataInicio;
            set
            {
                _dataInicio = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }

        public DateTime? DataFim
        {
            get => _dataFim;
            set
            {
                _dataFim = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }

        public ICommand GerarHtmlCommand { get; private set; }
        public ICommand LimparDatasCommand { get; private set; }
        public ICommand VoltarCommand { get; }

        private readonly RelatoriosViewModel _pai;


        public RelatorioAulasViewModel(RelatoriosViewModel pai)
        {
            _pai = pai;
            _relatorioService = new RelatorioService();
            DataInicio = null;
            DataFim = null;

            VoltarCommand = new RelayCommand(Voltar);
            GerarHtmlCommand = new RelayCommand(GerarHtml);
            LimparDatasCommand = new RelayCommand(ExecutarLimparDatas);

            InicializarComboBoxTurmas();
            _ = CarregarDadosIniciaisAsync();
        }

        private void Voltar()
        {
            _pai.ConteudoAtual = null;
        }

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.AulasCache ?? new List<AulaExibicaoDTO>();
                Aulas = _listaOriginalDoBanco;

                InicializarComboBoxTurmas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar aulas do cache: " + ex.Message);
            }
        }

        private void InicializarComboBoxTurmas()
        {
            var listaCombo = new List<TurmaExibicaoDTO>();
            listaCombo.Add(new TurmaExibicaoDTO
            {
                Nome = "Todas as turmas",
                TurmaOriginal = new Turma { Nome = "Todas as turmas" }
            });

            if (DataCacheService.TurmasCache != null)
            {
                listaCombo.AddRange(DataCacheService.TurmasCache);
            }

            Turmas = listaCombo;
            TurmaSelecionada = listaCombo[0];
        }

        private void ExecutarBusca()
        {
            try
            {
                string turmaFiltro = TurmaSelecionada?.Nome ?? "Todas as turmas";

                Aulas = _listaOriginalDoBanco.Where(a =>
                {
                    bool bateTurma = (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    bool bateDataInicio = !DataInicio.HasValue || a.Data.Date >= DataInicio.Value.Date;

                    bool bateDataFim = !DataFim.HasValue || a.Data.Date <= DataFim.Value.Date;

                    return bateTurma && bateDataInicio && bateDataFim;
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro durante a filtragem por nome de turma: " + ex.Message);
            }
        }

        private void ExecutarLimparDatas()
        {
            DataInicio = null;
            DataFim = null;
        }

        private void GerarHtml(object parametro)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Salvar relatorio HTML",
                    Filter = "Arquivos HTML (*.html)|*.html|Todos os arquivos (*.*)|*.*",
                    FileName = SanitizarNomeArquivo("Relatorio_Aula_" + AulaSelecionada.AulaId + "_" + AulaSelecionada.Turma + ".html")
                };

                if (dialog.ShowDialog() != true) return;

                var dados = _relatorioService.ObterDadosRelatorio(AulaSelecionada.AulaId);

                AlunosAusentes = dados ?? new List<RelatorioAusente>();
                // Usa as propriedades locais gerenciadas de forma independente pela Window
                string html = MontarHtml(AulaSelecionada, AlunosAusentes);

                File.WriteAllText(dialog.FileName, html, Encoding.UTF8);
                Process.Start(new ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });

                MessageBox.Show("Relatorio HTML gerado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar relatorio HTML: " + ex.Message);
            }
        }

        // 🌟 SEU MÉTODO ORIGINAL INTACTO (Copiado e colado linha por linha)
        private string MontarHtml(AulaExibicaoDTO aula, List<RelatorioAusente> ausentes)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine("    <title>Relatorio de Ausentes</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
            html.AppendLine("        h1 { margin-bottom: 8px; }");
            html.AppendLine("        .info { margin-bottom: 24px; font-size: 16px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; }");
            html.AppendLine("        th, td { border: 1px solid #cbd2d9; padding: 10px; text-align: left; }");
            html.AppendLine("        th { background: #f0f4f8; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <h1>Relatorio de Ausentes</h1>");
            html.AppendLine("    <div class=\"info\">");
            html.AppendLine("        <p><strong>Data da aula:</strong> " + aula.Data.ToString("dd/MM/yyyy") + "</p>");
            html.AppendLine("        <p><strong>Turma:</strong> " + WebUtility.HtmlEncode(aula.Turma) + "</p>");
            html.AppendLine("        <p><strong>Total de ausentes:</strong> " + aula.NumeroAusentes + "</p>");
            html.AppendLine("    </div>");

            if (ausentes == null || ausentes.Count == 0)
            {
                html.AppendLine("    <p>Nao houve ausencias nesta aula.</p>");
            }
            else
            {
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>Matricula</th>");
                html.AppendLine("                <th>Nome</th>");
                html.AppendLine("                <th>Email</th>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                foreach (RelatorioAusente ausente in ausentes)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Matricula) + "</td>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Aluno) + "</td>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Email) + "</td>");
                    html.AppendLine("            </tr>");
                }

                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private string SanitizarNomeArquivo(string nome)
        {
            foreach (char caractere in Path.GetInvalidFileNameChars())
            {
                nome = nome.Replace(caractere, '_');
            }
            return nome;
        }
    }
}