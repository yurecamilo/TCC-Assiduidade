using System;
using System.Collections.Generic;
using System.Linq;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.Relatorios;
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

        public ResultadoImportacaoCadastro Importacao(string turmaNome, List<Dictionary<string, string>> dados, DateTime? dataEntrada)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(turmaNome))
                {
                    return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = "Nome da turma inválido." };
                }

                if (dados == null || dados.Count == 0)
                {
                    return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = "Nenhum dado encontrado no arquivo CSV." };
                }

                string[] colunasObrigatorias = { "matricula", "nome", "email" };
                var primeiraLinha = dados.First();

                bool temTodasAsColunas = colunasObrigatorias.All(coluna =>
                    primeiraLinha.Keys.Any(chave => chave.Equals(coluna, StringComparison.OrdinalIgnoreCase))
                );

                if (!temTodasAsColunas)
                {
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = "O arquivo CSV não contém todas as colunas obrigatórias: matricula, nome, email."
                    };
                }

                List<string> matriculas = new List<string>();
                int linhaId = 1; // Declarado corretamente aqui

                foreach (var linha in dados)
                {
                    linhaId++;
                    // Busca as chaves de forma case-insensitive
                    string chaveMatricula = linha.Keys.FirstOrDefault(k => k.Equals("matricula", StringComparison.OrdinalIgnoreCase));
                    string matricula = chaveMatricula != null ? linha[chaveMatricula].Trim() : "";

                    if (string.IsNullOrWhiteSpace(matricula))
                    {
                        return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = $"Erro na linha {linhaId}: A matrícula não pode estar vazia." };
                    }
                    matriculas.Add(matricula);
                }

                int alunosExistencia = _alunoService.ObterQuantidadeMatriculasExistentes(matriculas);
                if (alunosExistencia > 0)
                {
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = $"Existem {alunosExistencia} alunos com matrícula já cadastrada no sistema."
                    };
                }

                if (dataEntrada.HasValue && dataEntrada.Value.Date > DateTime.Today)
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = "A data de entrada não pode ser futura."
                    };

                Turma turma = _turmaService.ObterTurmaPorNome(turmaNome);

                if (turma == null)
                {
                    _turmaService.Adicionar(turmaNome);
                    turma = _turmaService.ObterTurmaPorNome(turmaNome);
                }

                List<Aluno> alunosParaSalvar = new List<Aluno>();
                linhaId = 1;

                foreach (var linha in dados)
                {
                    linhaId++;
                    string chaveMatricula = linha.Keys.FirstOrDefault(k => k.Equals("matricula", StringComparison.OrdinalIgnoreCase));
                    string chaveNome = linha.Keys.FirstOrDefault(k => k.Equals("nome", StringComparison.OrdinalIgnoreCase));
                    string chaveEmail = linha.Keys.FirstOrDefault(k => k.Equals("email", StringComparison.OrdinalIgnoreCase));

                    string matricula = chaveMatricula != null ? WebUtilityTrim(linha[chaveMatricula]) : "";
                    string nome = chaveNome != null ? WebUtilityTrim(linha[chaveNome]) : "";
                    string email = chaveEmail != null ? WebUtilityTrim(linha[chaveEmail]) : "";

                    if (string.IsNullOrWhiteSpace(matricula) || string.IsNullOrWhiteSpace(nome))
                    {
                        return new ResultadoImportacaoCadastro
                        {
                            Sucesso = false,
                            Mensagem = $"Erro na linha {linhaId}: A matrícula e o nome do aluno não podem estar vazios."
                        };
                    }

                    var aluno = new Aluno
                    {
                        Matricula = matricula,
                        Nome = nome,
                        Email = email,
                        TurmaId = turma.Id,
                        DataEntrada = dataEntrada
                    };
                    alunosParaSalvar.Add(aluno);
                }

                _alunoService.Adicionar(alunosParaSalvar);

                return new ResultadoImportacaoCadastro
                {
                    Sucesso = true,
                    Mensagem = $"Turma {turma.Nome} processada com sucesso. {alunosParaSalvar.Count} alunos importados.",
                    Alunos = alunosParaSalvar
                };
            }
            catch (Exception ex)
            {
                return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = $"Erro inesperado: {ex.Message}" };
            }
        }

        public ResultadoImportacaoPresenca ImportarPresenca(int turmaId, List<Dictionary<string, string>> dadosPresenca)
        {
            try
            {
                if (dadosPresenca == null || dadosPresenca.Count == 0)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Nenhum dado de presença foi fornecido." };
                }

                // 1. Busca a data na primeira linha preenchida de verdade
                DateTime dataAula = DateTime.MinValue;
                bool dataEncontrada = false;

                foreach (var linha in dadosPresenca)
                {
                    if (linha.ContainsKey("data_presenca") && !string.IsNullOrWhiteSpace(linha["data_presenca"]))
                    {
                        if (DateTime.TryParse(linha["data_presenca"].Trim(), out dataAula))
                        {
                            dataEncontrada = true;
                            break;
                        }
                    }
                }

                if (!dataEncontrada)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Não foi possível identificar nenhuma data válida na coluna 'data_presenca'." };
                }

                if (_turmaService.ObterTurmaPorId(turmaId) == null)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Turma nao encontrada." };
                }

                string[] colunasObrigatorias = { "matricula_aluno", "email_aluno", "nome_aluno", "data_presenca" };
                var primeiraLinha = dadosPresenca.First();

                bool temTodasAsColunas = colunasObrigatorias.All(coluna =>
                    primeiraLinha.Keys.Any(chave => chave.Equals(coluna, StringComparison.OrdinalIgnoreCase))
                );

                if (!temTodasAsColunas)
                {
                    return new ResultadoImportacaoPresenca
                    {
                        Sucesso = false,
                        Mensagem = "O arquivo CSV não contém todas as colunas obrigatórias para presença: matricula_aluno, email_aluno, nome_aluno, data_presenca."
                    };
                }

                HashSet<string> matriculasPresentes = new HashSet<string>();
                int linhaId = 1;

                foreach (var linha in dadosPresenca)
                {
                    linhaId++;
                    string matricula = linha.ContainsKey("matricula_aluno") ? linha["matricula_aluno"].Trim() : "";

                    if (string.IsNullOrWhiteSpace(matricula))
                    {
                        return new ResultadoImportacaoPresenca
                        {
                            Sucesso = false,
                            Mensagem = $"Erro na linha {linhaId}: O campo 'matricula_aluno' não pode estar vazio."
                        };
                    }
                    matriculasPresentes.Add(matricula);
                }

                int alunosDeOutraTurma = _alunoService.ContarAlunosDeOutraTurma(turmaId, matriculasPresentes.ToList());
                if (alunosDeOutraTurma > 0)
                {
                    return new ResultadoImportacaoPresenca
                    {
                        Sucesso = false,
                        Mensagem = $"Existem {alunosDeOutraTurma} alunos no CSV que pertencem a uma turma diferente da selecionada."
                    };
                }

                List<Aluno> alunosDaTurma = _alunoService.ObterPorTurma(turmaId);
                if (alunosDaTurma.Count == 0)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Nenhum aluno encontrado na turma." };
                }

                List<string> ausentes = new List<string>();
                foreach (var aluno in alunosDaTurma)
                {
                    if (!matriculasPresentes.Contains(aluno.Matricula))
                    {
                        ausentes.Add(aluno.Matricula);
                    }
                }

                int aulaId = _aulaService.Adicionar(dataAula, turmaId);
                _ausenciaService.Adicionar(aulaId, ausentes);
                List<RelatorioAusente> relatorioAusentes = _relatorioService.ObterDadosRelatorio(aulaId);

                return new ResultadoImportacaoPresenca
                {
                    Sucesso = true,
                    Mensagem = "Presenca importada e ausencias registradas com sucesso!\n\n" +
                               $"Aula criada: {aulaId}\n" +
                               $"Data: {dataAula:dd/MM/yyyy}\n" +
                               $"Total de alunos da turma: {alunosDaTurma.Count}\n" +
                               $"Total de presentes no CSV: {matriculasPresentes.Count}\n" +
                               $"Total de ausentes salvos: {ausentes.Count}",
                    Ausentes = relatorioAusentes
                };
            }
            catch (Exception ex)
            {
                return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = $"Erro inesperado: {ex.Message}" };
            }
        }

        private string WebUtilityTrim(string valor)
        {
            return valor?.Trim() ?? "";
        }
    }
}