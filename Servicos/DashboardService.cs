using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Repositories;
using TCC_Assiduidade.Modelos.Banco;

namespace TCC_Assiduidade.Servicos
{
    public class DashboardService
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");
        private readonly DashboardRepository _dashboardRepository;
        private List<RegistroFrequenciaDashboard> _registros = new List<RegistroFrequenciaDashboard>();
        private bool _dadosCarregados;

        public DashboardService()
        {
            _dashboardRepository = new DashboardRepository();
        }

        internal DashboardService(IEnumerable<RegistroFrequenciaDashboard> registros)
        {
            _registros = registros?.ToList() ?? new List<RegistroFrequenciaDashboard>();
            _dadosCarregados = true;
        }

        public async Task CarregarDadosAsync()
        {
            if (_dadosCarregados)
                return;

            _registros = await Task.Run(() => _dashboardRepository.ObterRegistrosFrequencia());
            _dadosCarregados = true;
        }

        public List<OpcaoTurmaDashboard> ObterTurmas(bool incluirTodas)
        {
            var itens = _registros
                .GroupBy(r => new { r.TurmaId, r.Turma })
                .Select(g => new OpcaoTurmaDashboard { Id = g.Key.TurmaId, Nome = g.Key.Turma, Arquivada = g.Any(r => r.StatusTurma == EStatusTurma.Arquivada) })
                .OrderBy(t => t.Nome)
                .ToList();

            if (incluirTodas)
            {
                itens.Insert(0, new OpcaoTurmaDashboard { Id = 0, Nome = "Todas as turmas" });
            }

            return itens;
        }

        public List<OpcaoAlunoDashboard> ObterAlunos(int turmaId, bool incluirArquivadas = false)
        {
            return _registros
                .Where(r => turmaId > 0 ? r.TurmaId == turmaId : incluirArquivadas || r.StatusTurma == EStatusTurma.Ativa)
                .GroupBy(r => new { r.Matricula, r.Aluno, r.TurmaId, r.Turma })
                .Select(g => new OpcaoAlunoDashboard
                {
                    Matricula = g.Key.Matricula,
                    Nome = g.Key.Aluno,
                    TurmaId = g.Key.TurmaId,
                    Turma = g.Key.Turma
                })
                .OrderBy(a => a.Nome)
                .ToList();
        }

        public DateTime? ObterPrimeiraData()
        {
            return _registros.Count == 0 ? (DateTime?)null : _registros.Min(r => r.Data).Date;
        }

        public DateTime? ObterUltimaData()
        {
            return _registros.Count == 0 ? (DateTime?)null : _registros.Max(r => r.Data).Date;
        }

        public ResultadoDashboardAluno ObterDashboardAluno(
            string matricula,
            int turmaId,
            DateTime? dataInicio,
            DateTime? dataFim,
            bool incluirArquivadas = false)
        {
            var registrosAluno = FiltrarPeriodo(_registros, dataInicio, dataFim)
                .Where(r => r.Matricula == matricula &&
                            (turmaId > 0 ? r.TurmaId == turmaId : incluirArquivadas || r.StatusTurma == EStatusTurma.Ativa))
                .OrderBy(r => r.Data)
                .ToList();

            if (registrosAluno.Count == 0)
            {
                return new ResultadoDashboardAluno
                {
                    Mensagem = "Não há aulas registradas para o aluno e período selecionados."
                };
            }

            var primeiro = registrosAluno[0];
            int presentes = registrosAluno.Count(r => r.Presente);
            int total = registrosAluno.Count;
            double assiduidade = CalcularPercentual(presentes, total);

            int turmaAnalisada = turmaId > 0 ? turmaId : primeiro.TurmaId;
            var registrosTurma = FiltrarPeriodo(_registros, dataInicio, dataFim)
                .Where(r => turmaId > 0 ? r.TurmaId == turmaAnalisada :
                    (incluirArquivadas || r.StatusTurma == EStatusTurma.Ativa) && r.TurmaId == turmaAnalisada)
                .ToList();

            double mediaTurma = CalcularPercentual(
                registrosTurma.Count(r => r.Presente),
                registrosTurma.Count);

            var ranking = registrosTurma
                .GroupBy(r => r.Matricula)
                .Select(g => new
                {
                    Matricula = g.Key,
                    Assiduidade = CalcularPercentual(g.Count(r => r.Presente), g.Count())
                })
                .OrderByDescending(x => x.Assiduidade)
                .ThenBy(x => x.Matricula)
                .ToList();

            int posicao = ranking.FindIndex(x => x.Matricula == matricula) + 1;
            var mesesAluno = registrosAluno
                .GroupBy(r => new DateTime(r.Data.Year, r.Data.Month, 1))
                .OrderBy(g => g.Key)
                .ToList();

            var evolucao = new List<PontoGraficoDashboard>();
            foreach (var mes in mesesAluno)
            {
                var turmaNoMes = registrosTurma
                    .Where(r => r.Data.Year == mes.Key.Year && r.Data.Month == mes.Key.Month)
                    .ToList();

                evolucao.Add(new PontoGraficoDashboard
                {
                    Rotulo = mes.Key.ToString("MMM/yy", Cultura),
                    Valor = CalcularPercentual(mes.Count(r => r.Presente), mes.Count()),
                    ValorSecundario = CalcularPercentual(
                        turmaNoMes.Count(r => r.Presente),
                        turmaNoMes.Count)
                });
            }

            return new ResultadoDashboardAluno
            {
                TemDados = true,
                Aluno = primeiro.Aluno,
                Matricula = primeiro.Matricula,
                Turma = primeiro.Turma,
                Presentes = presentes,
                Ausentes = total - presentes,
                TotalAulas = total,
                Assiduidade = assiduidade,
                MediaTurma = mediaTurma,
                PosicaoTurma = posicao,
                TotalAlunosTurma = ranking.Count,
                Situacao = ObterSituacao(assiduidade),
                Evolucao = evolucao
            };
        }

        public ResultadoDashboardTurmas ObterDashboardTurmas(
            int turmaId,
            DateTime? dataInicio,
            DateTime? dataFim,
            bool incluirArquivadas = false)
        {
            var registros = FiltrarPeriodo(_registros, dataInicio, dataFim)
                .Where(r => turmaId > 0 ? r.TurmaId == turmaId : incluirArquivadas || r.StatusTurma == EStatusTurma.Ativa)
                .ToList();

            if (registros.Count == 0)
            {
                return new ResultadoDashboardTurmas
                {
                    Mensagem = "Não há dados para as turmas e período selecionados."
                };
            }

            var ranking = registros
                .GroupBy(r => new { r.TurmaId, r.Turma })
                .Select(g => new PontoGraficoDashboard
                {
                    Rotulo = g.Key.Turma,
                    Valor = CalcularPercentual(g.Count(r => r.Presente), g.Count())
                })
                .OrderByDescending(p => p.Valor)
                .ToList();

            int alunosEmRisco = registros
                .GroupBy(r => r.Matricula)
                .Count(g => CalcularPercentual(g.Count(r => r.Presente), g.Count()) < 75);

            var evolucao = registros
                .GroupBy(r => new DateTime(r.Data.Year, r.Data.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new PontoGraficoDashboard
                {
                    Rotulo = g.Key.ToString("MMM/yy", Cultura),
                    Valor = CalcularPercentual(g.Count(r => r.Presente), g.Count())
                })
                .ToList();

            return new ResultadoDashboardTurmas
            {
                TemDados = true,
                Presentes = registros.Count(r => r.Presente),
                Ausentes = registros.Count(r => !r.Presente),
                AssiduidadeGeral = CalcularPercentual(registros.Count(r => r.Presente), registros.Count),
                TotalTurmas = registros.Select(r => r.TurmaId).Distinct().Count(),
                TotalAlunos = registros.Select(r => r.Matricula).Distinct().Count(),
                AlunosEmRisco = alunosEmRisco,
                Ranking = ranking,
                Evolucao = evolucao
            };
        }

        public ResultadoDashboardAulas ObterDashboardAulas(
            int turmaId,
            DateTime? dataInicio,
            DateTime? dataFim,
            bool incluirArquivadas = false)
        {
            var registros = FiltrarPeriodo(_registros, dataInicio, dataFim)
                .Where(r => turmaId > 0 ? r.TurmaId == turmaId : incluirArquivadas || r.StatusTurma == EStatusTurma.Ativa)
                .ToList();

            if (registros.Count == 0)
            {
                return new ResultadoDashboardAulas
                {
                    Mensagem = "Não há aulas para a turma e período selecionados."
                };
            }

            var aulas = registros
                .GroupBy(r => new { r.AulaId, r.Data, r.Turma })
                .Select(g => new
                {
                    g.Key.AulaId,
                    g.Key.Data,
                    g.Key.Turma,
                    Presentes = g.Count(r => r.Presente),
                    Total = g.Count(),
                    Percentual = CalcularPercentual(g.Count(r => r.Presente), g.Count())
                })
                .OrderBy(a => a.Data)
                .ThenBy(a => a.AulaId)
                .ToList();

            var melhorAula = aulas.OrderByDescending(a => a.Percentual).ThenByDescending(a => a.Data).First();
            var aulaAtencao = aulas.OrderBy(a => a.Percentual).ThenBy(a => a.Data).First();
            int inicioUltimas = Math.Max(0, aulas.Count - 12);

            var pontosAula = aulas
                .Skip(inicioUltimas)
                .Select(a => new PontoGraficoDashboard
                {
                    Rotulo = a.Data.ToString("dd/MM"),
                    Valor = a.Percentual
                })
                .ToList();

            var evolucao = registros
                .GroupBy(r => new DateTime(r.Data.Year, r.Data.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new PontoGraficoDashboard
                {
                    Rotulo = g.Key.ToString("MMM/yy", Cultura),
                    Valor = CalcularPercentual(g.Count(r => r.Presente), g.Count())
                })
                .ToList();

            return new ResultadoDashboardAulas
            {
                TemDados = true,
                Presentes = registros.Count(r => r.Presente),
                Ausentes = registros.Count(r => !r.Presente),
                PresencaMedia = CalcularPercentual(registros.Count(r => r.Presente), registros.Count),
                TotalAulas = aulas.Count,
                TotalAusencias = registros.Count(r => !r.Presente),
                MelhorAula = FormatarAula(melhorAula.Data, melhorAula.Turma, melhorAula.Percentual),
                AulaAtencao = FormatarAula(aulaAtencao.Data, aulaAtencao.Turma, aulaAtencao.Percentual),
                Aulas = pontosAula,
                Evolucao = evolucao
            };
        }

        private static List<RegistroFrequenciaDashboard> FiltrarPeriodo(
            List<RegistroFrequenciaDashboard> registros,
            DateTime? dataInicio,
            DateTime? dataFim)
        {
            return registros.Where(r =>
                (!dataInicio.HasValue || r.Data.Date >= dataInicio.Value.Date) &&
                (!dataFim.HasValue || r.Data.Date <= dataFim.Value.Date))
                .ToList();
        }

        private static double CalcularPercentual(int valor, int total)
        {
            return total <= 0 ? 0 : Math.Round(valor * 100.0 / total, 1);
        }

        private static string FormatarAula(DateTime data, string turma, double percentual)
        {
            return data.ToString("dd/MM/yyyy") + " • " + turma + " • " +
                   percentual.ToString("0.#", Cultura) + "%";
        }

        private static string ObterSituacao(double assiduidade)
        {
            if (assiduidade < 75)
                return "Em risco";

            if (assiduidade < 85)
                return "Atenção";

            return "Regular";
        }
    }
}
