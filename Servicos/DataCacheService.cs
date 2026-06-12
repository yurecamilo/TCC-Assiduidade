using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;

namespace TCC_Assiduidade.Servicos
{
    public static class DataCacheService
    {
        private static readonly AlunoService _alunoService = new AlunoService();
        private static readonly TurmaService _turmaService = new TurmaService();
        private static readonly AulaService _aulaService = new AulaService();

        // 🌟 ADICIONADO: Evento para notificar os ViewModels (Telas) de que os dados mudaram
        public static event Action CacheAtualizado;

        // Propriedades globais que vão guardar os dados na memória RAM
        public static List<Turma> TurmaModeloCache { get; private set; } = new List<Turma>();
        public static List<AlunoExibicaoDTO> AlunosCache { get; private set; } = new List<AlunoExibicaoDTO>();
        public static List<TurmaExibicaoDTO> TurmasCache { get; private set; } = new List<TurmaExibicaoDTO>();
        public static List<AulaExibicaoDTO> AulasCache { get; private set; } = new List<AulaExibicaoDTO>();

        // Flag para sabermos se o cache já foi carregado com sucesso
        public static bool IsCarregado { get; private set; } = false;

        /// <summary>
        /// Dispara a carga de todas as tabelas em paralelo sem travar o sistema
        /// </summary>
        public static void InicializarCargaBackground()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Roda a busca de alunos e turmas em paralelo no banco de dados
                    var tarefaAlunos = Task.Run(() => _alunoService.ObterPerfilAluno());
                    var tarefaTurmas = Task.Run(() => _turmaService.ObterTurmasComContagem());
                    var tarefaAulas = Task.Run(() => _aulaService.ObterResumoAulas());
                    var tarefaTurmaModelo = Task.Run(() => _turmaService.ObterTodasTurmas());

                    // Aguarda ambas terminarem
                    await Task.WhenAll(tarefaAlunos, tarefaTurmas, tarefaAulas, tarefaTurmaModelo);

                    // Salva o resultado na memória global
                    AlunosCache = tarefaAlunos.Result ?? new List<AlunoExibicaoDTO>();
                    TurmasCache = tarefaTurmas.Result ?? new List<TurmaExibicaoDTO>();
                    AulasCache = tarefaAulas.Result ?? new List<AulaExibicaoDTO>();
                    TurmaModeloCache = tarefaTurmaModelo.Result ?? new List<Turma>();

                    IsCarregado = true;

                    // 🌟 ADICIONADO: Avisa as telas que a carga inicial terminou
                    CacheAtualizado?.Invoke();
                }
                catch (Exception)
                {
                    // Trate o erro de conexão aqui se necessário, 
                    // para evitar que o app feche na inicialização
                    IsCarregado = false;
                }
            });
        }

        /// <summary>
        /// Método utilitário caso você precise forçar a atualização (Refresh) do cache manualmente
        /// </summary>
        public static async Task ForçarAtualizacaoAsync()
        {
            // 🌟 CORRIGIDO: Adicionado a busca de Aulas e TurmaModelo para o refresh não ficar incompleto
            var tarefaAlunos = Task.Run(() => _alunoService.ObterPerfilAluno());
            var tarefaTurmas = Task.Run(() => _turmaService.ObterTurmasComContagem());
            var tarefaAulas = Task.Run(() => _aulaService.ObterResumoAulas());
            var tarefaTurmaModelo = Task.Run(() => _turmaService.ObterTodasTurmas());

            // Aguarda todas terminarem
            await Task.WhenAll(tarefaAlunos, tarefaTurmas, tarefaAulas, tarefaTurmaModelo);

            // Atualiza os dados
            AlunosCache = tarefaAlunos.Result ?? new List<AlunoExibicaoDTO>();
            TurmasCache = tarefaTurmas.Result ?? new List<TurmaExibicaoDTO>();
            AulasCache = tarefaAulas.Result ?? new List<AulaExibicaoDTO>();
            TurmaModeloCache = tarefaTurmaModelo.Result ?? new List<Turma>();

            IsCarregado = true;

            // 🌟 ADICIONADO: Avisa as telas (Alunos, Turmas, Dashboard) que os dados foram recarregados
            CacheAtualizado?.Invoke();
        }
    }
}