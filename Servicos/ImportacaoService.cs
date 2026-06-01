using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Modelos.Resultados;

namespace TCC_Assiduidade.Servicos
{
    public class ImportacaoService
    {
        private readonly AlunoService _alunoService;
        private readonly TurmaService _turmaService;
        private readonly AulaService _aulaService;
        private readonly AusenciaService _ausenciaService;
        private readonly RelatorioService _relatorioService;

        public ImportacaoService()
        {
            _alunoService = new AlunoService();
            _turmaService = new TurmaService();
            _aulaService = new AulaService();
            _ausenciaService = new AusenciaService();
            _relatorioService = new RelatorioService();
        }

        public ResultadoImportacaoCadastro Importacao(string turmaNome, List<Dictionary<string, string>> dados)
        {
            if (string.IsNullOrWhiteSpace(turmaNome))
            {
                return new ResultadoImportacaoCadastro
                {
                    Sucesso = false,
                    Mensagem = "Nome da turma invalido."
                };
            }

            Turma turma = _turmaService.ObterTurmaPorNome(turmaNome);

            if (turma == null)
            {
                int turmaId = _turmaService.Adicionar(turmaNome);
                if (turmaId == -1)
                {
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao criar turma."
                    };
                }

                turma = new Turma { Id = turmaId, Nome = turmaNome };
            }
            //else
            //{
            //    return new ResultadoImportacaoCadastro
            //    {
            //        Sucesso = false,
            //        Mensagem = $"A turma {turma.Nome} ja esta cadastrada no sistema."
            //    };
            //}

            List<Aluno> alunosParaSalvar = new List<Aluno>();

            foreach (var linha in dados)
            {
                var aluno = new Aluno
                {
                    Matricula = linha["matricula"],
                    Nome = linha["nome"],
                    Email = linha.ContainsKey("email") ? linha["email"] : "",
                    TurmaId = turma.Id
                };

                alunosParaSalvar.Add(aluno);
            }

            _alunoService.Adicionar(alunosParaSalvar);

            return new ResultadoImportacaoCadastro
            {
                Sucesso = true,
                Mensagem = $"Turma {turma.Nome} criada com sucesso. {alunosParaSalvar.Count} alunos importados.",
                Alunos = alunosParaSalvar
            };
        }

        public ResultadoImportacaoPresenca ImportarPresenca(int turmaId, List<Dictionary<string, string>> dadosPresenca)
        {
            DateTime dataAula;
            DateTime.TryParse(dadosPresenca[0]["data_presenca"], out dataAula);

            if (_turmaService.ObterTurmaPorId(turmaId) == null)
            {
                return new ResultadoImportacaoPresenca
                {
                    Sucesso = false,
                    Mensagem = "Turma nao encontrada."
                };
            }

            int aulaId = _aulaService.Adicionar(dataAula, turmaId);

            if (aulaId == -1)
            {
                return new ResultadoImportacaoPresenca
                {
                    Sucesso = false,
                    Mensagem = "Erro ao criar aula."
                };
            }

            List<Aluno> alunosDaTurma = _alunoService.ObterPorTurma(turmaId);
            if (alunosDaTurma.Count == 0)
            {
                return new ResultadoImportacaoPresenca
                {
                    Sucesso = false,
                    Mensagem = "Nenhum aluno encontrado na turma."
                };
            }

            HashSet<string> matriculasPresentes = new HashSet<string>();

            foreach (var linha in dadosPresenca)
            {
                if (linha.ContainsKey("matricula_aluno"))
                {
                    string matricula = linha["matricula_aluno"].Trim();
                    matriculasPresentes.Add(matricula);
                }
            }

            List<string> ausentes = new List<string>();

            foreach (var aluno in alunosDaTurma)
            {
                if (!matriculasPresentes.Contains(aluno.Matricula))
                {
                    ausentes.Add(aluno.Matricula);
                }
            }

            _ausenciaService.Adicionar(aulaId, ausentes);

            List<RelatorioAusente> relatorioAusentes = _relatorioService.ObterDadosRelatorio(aulaId);

            return new ResultadoImportacaoPresenca
            {
                Sucesso = true,
                Mensagem =
                    "Presenca importada e ausencias registradas com sucesso!\n\n" +
                    $"Aula criada: {aulaId}\n" +
                    $"Data: {dataAula:dd/MM/yyyy}\n" +
                    $"Total de alunos da turma: {alunosDaTurma.Count}\n" +
                    $"Total de presentes no CSV: {matriculasPresentes.Count}\n" +
                    $"Total de ausentes salvos: {ausentes.Count}",
                Ausentes = relatorioAusentes
            };
        }
    }
}
