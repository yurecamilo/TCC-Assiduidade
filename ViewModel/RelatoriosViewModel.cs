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
    // 🌟 CLASSE AUXILIAR ADICIONADA: Representa o item selecionável no segundo ComboBox
    public class AlvoItem
    {
        public string Id { get; set; }          // Pode ser a Matrícula do aluno ou o Nome da turma
        public string NomeExibicao { get; set; } // O que aparece escrito na tela para o professor
    }

    public class RelatoriosViewModel : BaseViewModel
    {
        private readonly RelatorioService _relatorioService;

        private string _tipoRelatorioSelecionado;
        private List<AlvoItem> _itensFiltrados;
        private AlvoItem _itemSelecionado;

        public string TextoExibicaoAluno;
        // 🌟 PROPRIEDADE: Monitora se o usuário escolheu "Por Turma" ou "Por Aluno"
        public string TipoRelatorioSelecionado
        {
            get => _tipoRelatorioSelecionado;
            set
            {
                _tipoRelatorioSelecionado = value;
                OnPropertyChanged();
                AtualizarListaAlvos(); // Recarrega o segundo ComboBox dinamicamente
            }
        }

        // 🌟 PROPRIEDADE: Alimenta o segundo ComboBox com Alunos ou Turmas
        public List<AlvoItem> ItensFiltrados
        {
            get => _itensFiltrados;
            set { _itensFiltrados = value; OnPropertyChanged(); }
        }

        // 🌟 PROPRIEDADE: Guarda o Aluno ou Turma que o professor escolheu para o relatório
        public AlvoItem ItemSelecionado
        {
            get => _itemSelecionado;
            set { _itemSelecionado = value; OnPropertyChanged(); }
        }

        public ICommand GerarHtmlCommand { get; private set; }

        public RelatoriosViewModel()
        {
            _relatorioService = new RelatorioService();
            GerarHtmlCommand = new RelayCommand(GerarHtml);

            // Garante que o cache do sistema já carregou antes de listar os dados
            _ = InicializarDadosAsync();
        }

        private async Task InicializarDadosAsync()
        {
            while (!DataCacheService.IsCarregado)
            {
                await Task.Delay(100);
            }

            // 🌟 ALTERADO: Começa totalmente limpo (null) para obedecer os placeholders do XAML
            TipoRelatorioSelecionado = null;
            ItensFiltrados = new List<AlvoItem>();
            ItemSelecionado = null;
        }

        private void AtualizarListaAlvos()
        {
            var novaLista = new List<AlvoItem>();

            // Se o professor ainda não escolheu o tipo, deixa a lista de alvos vazia
            if (string.IsNullOrEmpty(TipoRelatorioSelecionado))
            {
                ItensFiltrados = novaLista;
                ItemSelecionado = null;
                return;
            }

            if (TipoRelatorioSelecionado == "Por Turma" && DataCacheService.TurmasCache != null)
            {
                foreach (var turma in DataCacheService.TurmasCache)
                {
                    novaLista.Add(new AlvoItem { Id = turma.Nome, NomeExibicao = turma.Nome });
                }
            }
            else if (TipoRelatorioSelecionado == "Por Aluno" && DataCacheService.AlunosCache != null)
            {
                foreach (var aluno in DataCacheService.AlunosCache)
                {
                    // 🌟 ALTERADO: Exibição no padrão "Matrícula - Nome Completo (Turma)"
                    novaLista.Add(new AlvoItem
                    {
                        Id = aluno.Matricula,
                        NomeExibicao = $"{aluno.Matricula} - {aluno.Nome} ({aluno.Turma})"
                    });
                }
            }

            ItensFiltrados = novaLista;

            // 🌟 ALTERADO: Não seleciona o primeiro automaticamente. Deixa nulo para o usuário escolher.
            ItemSelecionado = null;
        }

        // 🌟 ADAPTADO: Agora lê a escolha do formulário e monta o PDF/HTML sob demanda
        private void GerarHtml(object parametro)
        {
            try
            {
                if (ItemSelecionado == null)
                {
                    MessageBox.Show("Por favor, selecione um alvo para gerar o relatório.");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Salvar relatório HTML",
                    Filter = "Arquivos HTML (*.html)|*.html|Todos os arquivos (*.*)|*.*",
                    FileName = SanitizarNomeArquivo($"Relatorio_{TipoRelatorioSelecionado.Replace(" ", "")}_{ItemSelecionado.Id}.html")
                };

                if (dialog.ShowDialog() != true) return;

                string htmlGenerated = string.Empty;

                // Divide a busca no banco/cache baseado na escolha do formulário
                if (TipoRelatorioSelecionado == "Por Turma")
                {
                    // Busca todas as aulas que pertencem àquela turma específica
                    List<AulaExibicaoDTO> aulasDaTurma = DataCacheService.AulasCache?
                        .Where(a => a.Turma != null && a.Turma.Equals(ItemSelecionado.Id, StringComparison.OrdinalIgnoreCase))
                        .ToList() ?? new List<AulaExibicaoDTO>();

                    htmlGenerated = MontarHtmlTurma(ItemSelecionado.Id, aulasDaTurma);
                }
                else
                {
                    // Busca a ficha do Aluno Selecionado no Cache
                    var aluno = DataCacheService.AlunosCache?.FirstOrDefault(al => al.Matricula == ItemSelecionado.Id);
                    htmlGenerated = MontarHtmlAluno(aluno);
                }

                File.WriteAllText(dialog.FileName, htmlGenerated, Encoding.UTF8);
                Process.Start(new ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });

                MessageBox.Show("Relatório gerado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar relatório: " + ex.Message);
            }
        }

        // 🌟 NOVO LAYOUT HTML: Consolida os dados estatísticos da Turma completa
        private string MontarHtmlTurma(string nomeTurma, List<AulaExibicaoDTO> aulas)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine($"    <title>Relatório Geral - {nomeTurma}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
            html.AppendLine("        h1 { margin-bottom: 8px; color: #236B73; }");
            html.AppendLine("        .info { margin-bottom: 24px; font-size: 16px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            html.AppendLine("        th, td { border: 1px solid #cbd2d9; padding: 10px; text-align: left; }");
            html.AppendLine("        th { background: #f0f4f8; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>Relatório de Histórico por Turma</h1>");
            html.AppendLine("    <div class=\"info\">");
            html.AppendLine($"        <p><strong>Turma analisada:</strong> {WebUtility.HtmlEncode(nomeTurma)}</p>");
            html.AppendLine($"        <p><strong>Total de diários de classe mapeados:</strong> {aulas.Count}</p>");
            html.AppendLine("    </div>");

            if (aulas.Count == 0)
            {
                html.AppendLine("    <p>Não há histórico de chamadas salvas para esta turma no sistema.</p>");
            }
            else
            {
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>Data da Aula</th>");
                html.AppendLine("                <th>Número Absoluto de Ausências</th>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                foreach (var aula in aulas)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td>{aula.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"                <td>{aula.NumeroAusentes} alunos faltantes</td>");
                    html.AppendLine("            </tr>");
                }
                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
            }

            html.AppendLine("</body></html>");
            return html.ToString();
        }

        // 🌟 NOVO LAYOUT HTML: Gera uma Ficha Individual do Aluno contendo o seu percentual de Faltas/Assiduidade
        private string MontarHtmlAluno(AlunoExibicaoDTO aluno)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
            html.AppendLine("        h1 { margin-bottom: 8px; color: #236B73; }");
            html.AppendLine("        .card-perf { background: #ECF8F6; border-radius: 8px; padding: 20px; border: 1px solid #B0C8D6; margin-top: 15px; }");
            html.AppendLine("        p { font-size: 15px; line-height: 1.6; margin: 6px 0; }");
            html.AppendLine("        .destaque { font-size: 20px; font-weight: bold; color: #236B73; }");
            html.AppendLine("        .falta { color: #AA3333; font-weight: bold; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>Ficha de Rendimento e Assiduidade</h1>");

            if (aluno == null)
            {
                html.AppendLine("    <p>Dados cadastrais do aluno não localizados.</p>");
            }
            else
            {
                html.AppendLine("    <div class=\"card-perf\">");
                html.AppendLine($"        <p><strong>Nome Completo:</strong> {WebUtility.HtmlEncode(aluno.Nome)}</p>");
                html.AppendLine($"        <p><strong>Número de Matrícula:</strong> {WebUtility.HtmlEncode(aluno.Matricula)}</p>");
                html.AppendLine($"        <p><strong>Vínculo de Turma:</strong> {WebUtility.HtmlEncode(aluno.Turma)}</p>");
                html.AppendLine($"        <p><strong>E-mail Institucional:</strong> {WebUtility.HtmlEncode(aluno.Email)}</p>");
                html.AppendLine("        <hr style='border: 0; border-top: 1px solid #B0C8D6; margin: 15px 0;'>");
                html.AppendLine($"        <p class='destaque'>Assiduidade Geral do Aluno: {aluno.DadosFrequencia?.Assiduidade}%</p>");
                html.AppendLine($"        <p>Total de Aulas Computadas: {aluno.DadosFrequencia?.TotalAulas} aulas</p>");
                html.AppendLine($"        <p class='falta'>Total de Ausências Registradas: {aluno.DadosFrequencia?.TotalFaltas} faltas</p>");
                html.AppendLine("    </div>");
            }

            html.AppendLine("</body></html>");
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