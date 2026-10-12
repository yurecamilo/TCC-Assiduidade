using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public static class DataCacheService
    {
        private static readonly AlunoService _alunoService = new AlunoService();
        private static readonly TurmaService _turmaService = new TurmaService();
        private static readonly AulaService _aulaService = new AulaService();

        public static event Action CacheAtualizado;

        public static List<Turma> TurmaModeloCache { get; private set; } = new List<Turma>();
        public static List<AlunoExibicaoDTO> AlunosCache { get; private set; } = new List<AlunoExibicaoDTO>();
        public static List<AlunoExibicaoDTO> AlunosHistoricoCache { get; private set; } = new List<AlunoExibicaoDTO>();
        public static List<TurmaExibicaoDTO> TurmasCache { get; private set; } = new List<TurmaExibicaoDTO>();
        public static List<TurmaExibicaoDTO> TurmasHistoricoCache { get; private set; } = new List<TurmaExibicaoDTO>();
        public static List<AulaExibicaoDTO> AulasCache { get; private set; } = new List<AulaExibicaoDTO>();

        public static bool IsCarregado { get; private set; } = false;
        public static Exception ErroCarregamento { get; private set; }

        /// <summary>
        /// Dispara a carga de todas as tabelas em paralelo sem travar o sistema
        /// </summary>
        public static void InicializarCargaBackground()
        {
            Task.Run(async () =>
            {
                try
                {
                    var tarefaAlunos = Task.Run(() => _alunoService.ObterPerfilAluno());
                    var tarefaAlunosHistorico = Task.Run(() => _alunoService.ObterPerfilAluno(incluirArquivados: true));
                    var tarefaTurmas = Task.Run(() => _turmaService.ObterTurmasComContagem(incluirArquivadas: true));
                    var tarefaAulas = Task.Run(() => _aulaService.ObterResumoAulas());
                    var tarefaTurmaModelo = Task.Run(() => _turmaService.ObterTurmasComContagem(incluirArquivadas: true)
                        .Where(t => t.TurmaOriginal.Status == EStatusTurma.Ativa)
                        .Select(t => t.TurmaOriginal).ToList());

                    await Task.WhenAll(tarefaAlunos, tarefaAlunosHistorico, tarefaTurmas, tarefaAulas, tarefaTurmaModelo);

                    AlunosCache = tarefaAlunos.Result ?? new List<AlunoExibicaoDTO>();
                    AlunosHistoricoCache = tarefaAlunosHistorico.Result ?? new List<AlunoExibicaoDTO>();
                    TurmasHistoricoCache = tarefaTurmas.Result ?? new List<TurmaExibicaoDTO>();
                    TurmasCache = FiltrarTurmasAtivas(TurmasHistoricoCache);
                    AulasCache = tarefaAulas.Result ?? new List<AulaExibicaoDTO>();
                    TurmaModeloCache = tarefaTurmaModelo.Result ?? new List<Turma>();

                    ErroCarregamento = null;
                    IsCarregado = true;

                    CacheAtualizado?.Invoke();
                }
                catch (Exception ex)
                {
                    AlunosCache = new List<AlunoExibicaoDTO>();
                    AlunosHistoricoCache = new List<AlunoExibicaoDTO>();
                    TurmasCache = new List<TurmaExibicaoDTO>();
                    TurmasHistoricoCache = new List<TurmaExibicaoDTO>();
                    AulasCache = new List<AulaExibicaoDTO>();
                    TurmaModeloCache = new List<Turma>();

                    ErroCarregamento = MySqlExceptionTranslator.TraduzirSeNecessario(
                        ex,
                        "os dados iniciais do sistema");
                    IsCarregado = true;
                    CacheAtualizado?.Invoke();
                }
            });
        }

        public static async Task AguardarCarregamentoAsync()
        {
            DateTime limite = DateTime.UtcNow.AddSeconds(40);
            while (!IsCarregado && DateTime.UtcNow < limite)
                await Task.Delay(100);

            if (!IsCarregado)
                throw new TimeoutException("O carregamento inicial do banco excedeu 40 segundos.");

            if (ErroCarregamento != null)
                throw ErroCarregamento;
        }

        /// <summary>
        /// Método utilitário caso você precise forçar a atualização (Refresh) do cache manualmente
        /// </summary>
        public static async Task ForçarAtualizacaoAsync()
        {
            try
            {
                var tarefaAlunos = Task.Run(() => _alunoService.ObterPerfilAluno());
                var tarefaAlunosHistorico = Task.Run(() => _alunoService.ObterPerfilAluno(incluirArquivados: true));
                var tarefaTurmas = Task.Run(() => _turmaService.ObterTurmasComContagem(incluirArquivadas: true));
                var tarefaAulas = Task.Run(() => _aulaService.ObterResumoAulas());
                var tarefaTurmaModelo = Task.Run(() => _turmaService.ObterTurmasComContagem(incluirArquivadas: true)
                    .Where(t => t.TurmaOriginal.Status == EStatusTurma.Ativa)
                    .Select(t => t.TurmaOriginal).ToList());

                await Task.WhenAll(tarefaAlunos, tarefaAlunosHistorico, tarefaTurmas, tarefaAulas, tarefaTurmaModelo);

                AlunosCache = tarefaAlunos.Result ?? new List<AlunoExibicaoDTO>();
                AlunosHistoricoCache = tarefaAlunosHistorico.Result ?? new List<AlunoExibicaoDTO>();
                TurmasHistoricoCache = tarefaTurmas.Result ?? new List<TurmaExibicaoDTO>();
                TurmasCache = FiltrarTurmasAtivas(TurmasHistoricoCache);
                AulasCache = tarefaAulas.Result ?? new List<AulaExibicaoDTO>();
                TurmaModeloCache = tarefaTurmaModelo.Result ?? new List<Turma>();

                ErroCarregamento = null;
                IsCarregado = true;

                CacheAtualizado?.Invoke();
            }
            catch (Exception ex)
            {
                ErroCarregamento = MySqlExceptionTranslator.TraduzirSeNecessario(
                    ex,
                    "os dados do sistema");
                throw ErroCarregamento;
            }
        }

        internal static List<TurmaExibicaoDTO> FiltrarTurmasAtivas(IEnumerable<TurmaExibicaoDTO> turmas)
        {
            return turmas.Where(t => t.TurmaOriginal != null && t.TurmaOriginal.Status == EStatusTurma.Ativa).ToList();
        }
    }
}
