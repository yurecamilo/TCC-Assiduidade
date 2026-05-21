using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos;

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

        public string Importacao(string turmaNome, List<Dictionary<string, string>> dados)
        {
            string resposta = "";

            if (string.IsNullOrWhiteSpace(turmaNome))
            {
                return "Nome da turma inválido.";
            }

            Turma turma = _turmaService.ObterTurmaPorNome(turmaNome);

            if (turma == null)
            {
                int turmaId = _turmaService.Adicionar(turmaNome);
                if (turmaId == -1) 
                    return "Erro ao criar turma.";

                turma = new Turma { Id = turmaId, Nome = turmaNome };

                resposta += $"Turma {turma.Nome} criada com sucesso. ";
            }
            else
            {
                return $"A turma {turma.Nome} já está cadastrada no sistema.";
            }

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

                resposta += $"\n- {aluno.Nome} (Matricula: {aluno.Matricula})";
            }

            _alunoService.Adicionar(alunosParaSalvar);

            return resposta;
        }


        public string ImportarPresenca(int turmaId, List<Dictionary<string, string>> dadosPresenca)
        {
            string resposta = "";
            DateTime dataAula;
            DateTime.TryParse(dadosPresenca[0]["data_presenca"], out dataAula);

            if (_turmaService.ObterTurmaPorId(turmaId) == null)
            {
                return "Turma não encontrada.";
            }

            int aulaId = _aulaService.Adicionar(dataAula, turmaId);

            if (aulaId == -1)
            {
                return "Erro ao criar aula.";
            }

            List<Aluno> alunosDaTurma = _alunoService.ObterPorTurma(turmaId);
            if (alunosDaTurma.Count == 0)
            {
                return "Nenhum aluno encontrado na turma.";
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

            resposta += "Presença importada e ausências registradas com sucesso!";
            resposta+= "\n\n------------------------------------\n\n";
            resposta += $"Aula criada: {aulaId}\n";
            resposta += $"Data: {dataAula:dd/MM/yyyy}\n";
            resposta += $"Total de alunos da turma: {alunosDaTurma.Count}\n";
            resposta += $"Total de presentes no CSV: {matriculasPresentes.Count}\n";
            resposta += $"Total de ausentes salvos: {ausentes.Count}";
            resposta += "\n\n------------------------------------\n\n";
            resposta += "Alunos ausentes:\n";

            List<RelatorioAusente> relatorioAusentes = _relatorioService.ObterDadosRelatorio(aulaId);
            foreach (var ausente in relatorioAusentes)
            {
                resposta += $"- {ausente.Aluno} (Matrícula: {ausente.Matricula}, Email: {ausente.Email})\n";
            }
            return resposta;
        }
    }
}
