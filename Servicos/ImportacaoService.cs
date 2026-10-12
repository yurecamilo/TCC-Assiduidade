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
                    return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = "Nome da turma inválido. Nenhum dado foi importado." };
                }

                if (dados == null || dados.Count == 0)
                {
                    return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = "Nenhum dado encontrado no arquivo CSV. Nenhum dado foi importado." };
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
                        Mensagem = "O arquivo CSV não contém todas as colunas obrigatórias: matricula, nome, email. Nenhum dado foi importado."
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
                        return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = $"Erro na linha {linhaId}: a matrícula não pode estar vazia. Nenhum dado foi importado." };
                    }
                    matriculas.Add(matricula);
                }

                if (matriculas.Distinct(StringComparer.OrdinalIgnoreCase).Count() != matriculas.Count)
                {
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = "O arquivo contém matrículas repetidas. Nenhum dado foi importado."
                    };
                }

                if (dataEntrada.HasValue && dataEntrada.Value.Date > DateTime.Today)
                    return new ResultadoImportacaoCadastro
                    {
                        Sucesso = false,
                        Mensagem = "A data de entrada não pode ser futura. Nenhum dado foi importado."
                    };

                Turma turma = _turmaService.ObterTurmaPorNome(turmaNome);
                if (turma != null) TurmaService.ValidarTurmaAtiva(turma);

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

                    if (string.IsNullOrWhiteSpace(matricula) || string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
                    {
                        return new ResultadoImportacaoCadastro
                        {
                            Sucesso = false,
                            Mensagem = $"Erro na linha {linhaId}: matrícula, nome e e-mail são obrigatórios. Nenhum dado foi importado."
                        };
                    }
                    if (!_alunoService.ValidarEmailInstitucional(email))
                        return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = $"Erro na linha {linhaId}: e-mail inválido. Nenhum dado foi importado." };

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

                _alunoService.ImportarCadastroAtomico(turmaNome, alunosParaSalvar, turma == null);

                return new ResultadoImportacaoCadastro
                {
                    Sucesso = true,
                    Mensagem = $"Turma {turma.Nome} processada com sucesso. {alunosParaSalvar.Count} alunos importados.",
                    Alunos = alunosParaSalvar
                };
            }
            catch (Exception ex)
            {
                return new ResultadoImportacaoCadastro { Sucesso = false, Mensagem = $"Importação cancelada: {ex.Message} Nenhum dado foi importado." };
            }
        }

        public ResultadoImportacaoPresenca ImportarPresenca(int turmaId, List<Dictionary<string, string>> dadosPresenca)
        {
            try
            {
                if (dadosPresenca == null || dadosPresenca.Count == 0)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Nenhum dado de presença foi fornecido. Nenhum dado foi importado." };
                }

                // Uma importação representa uma única aula. Valida todas as linhas
                // antes de consultar ou gravar dados, sem ignorar datas inválidas.
                DateTime dataAula = ObterDataPresenca(dadosPresenca);

                var turmaPresenca = _turmaService.ObterTurmaPorId(turmaId);
                if (turmaPresenca == null)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Turma não encontrada. Nenhum dado foi importado." };
                }
                TurmaService.ValidarTurmaAtiva(turmaPresenca);

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
                        Mensagem = "O arquivo CSV não contém todas as colunas obrigatórias para presença: matricula_aluno, email_aluno, nome_aluno, data_presenca. Nenhum dado foi importado."
                    };
                }

                HashSet<string> matriculasPresentes = new HashSet<string>();
                int linhaId = 1;

                foreach (var linha in dadosPresenca)
                {
                    linhaId++;
                    string chaveMatricula = linha.Keys.FirstOrDefault(k => k.Equals("matricula_aluno", StringComparison.OrdinalIgnoreCase));
                    string matricula = chaveMatricula != null ? WebUtilityTrim(linha[chaveMatricula]) : "";

                    if (string.IsNullOrWhiteSpace(matricula))
                    {
                        return new ResultadoImportacaoPresenca
                        {
                            Sucesso = false,
                            Mensagem = $"Erro na linha {linhaId}: o campo 'matricula_aluno' não pode estar vazio. Nenhum dado foi importado."
                        };
                    }
                    string chaveNome = linha.Keys.FirstOrDefault(k => k.Equals("nome_aluno", StringComparison.OrdinalIgnoreCase));
                    string chaveEmail = linha.Keys.FirstOrDefault(k => k.Equals("email_aluno", StringComparison.OrdinalIgnoreCase));
                    string nomeLinha = chaveNome == null ? "" : WebUtilityTrim(linha[chaveNome]);
                    string emailLinha = chaveEmail == null ? "" : WebUtilityTrim(linha[chaveEmail]);
                    if (string.IsNullOrWhiteSpace(nomeLinha) || string.IsNullOrWhiteSpace(emailLinha) || !_alunoService.ValidarEmailInstitucional(emailLinha))
                        return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = $"Erro na linha {linhaId}: nome e e-mail válidos são obrigatórios. Nenhum dado foi importado." };
                    if (!matriculasPresentes.Add(matricula))
                        return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = $"Erro na linha {linhaId}: matrícula repetida no arquivo. Nenhum dado foi importado." };
                }

                int alunosDeOutraTurma = _alunoService.ContarAlunosDeOutraTurma(turmaId, matriculasPresentes.ToList());
                if (alunosDeOutraTurma > 0)
                {
                    return new ResultadoImportacaoPresenca
                    {
                        Sucesso = false,
                        Mensagem = $"Existem {alunosDeOutraTurma} alunos no CSV que pertencem a uma turma diferente da selecionada. Nenhum dado foi importado."
                    };
                }

                List<Aluno> alunosDaTurma = _alunoService.ObterPorTurma(turmaId) ?? new List<Aluno>();
                if (alunosDaTurma.Count == 0)
                {
                    return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = "Nenhum aluno encontrado na turma. Nenhum dado foi importado." };
                }

                List<string> ausentes = ValidarEObterAusentes(dataAula, matriculasPresentes, alunosDaTurma);

                int aulaId = _alunoService.ImportarAulaEAusenciasAtomico(dataAula, turmaId, matriculasPresentes.ToList());
                List<RelatorioAusente> relatorioAusentes = alunosDaTurma
                    .Where(a => ausentes.Contains(a.Matricula, StringComparer.OrdinalIgnoreCase))
                    .Select(a => new RelatorioAusente
                    {
                        AulaId = aulaId,
                        Data = dataAula,
                        Turma = turmaPresenca.NomeExibicao,
                        Matricula = a.Matricula,
                        Aluno = a.Nome,
                        Email = a.Email
                    }).ToList();

                return new ResultadoImportacaoPresenca
                {
                    Sucesso = true,
                    Mensagem = "Presenca importada e ausencias registradas com sucesso!\n\n" +
                               $"Aula criada: {aulaId}\n" +
                               $"Data: {dataAula:dd/MM/yyyy}\n" +
                               $"Total de alunos vinculados na data da aula: {matriculasPresentes.Count + ausentes.Count}\n" +
                               $"Total de presentes no CSV: {matriculasPresentes.Count}\n" +
                               $"Total de ausentes salvos: {ausentes.Count}",
                    Ausentes = relatorioAusentes
                };
            }
            catch (ArgumentException ex)
            {
                return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = $"Importação cancelada: {ex.Message} Nenhum dado foi importado." };
            }
            catch (Exception ex)
            {
                return new ResultadoImportacaoPresenca { Sucesso = false, Mensagem = $"Importação cancelada por erro: {ex.Message} Nenhum dado foi importado." };
            }
        }

        internal static DateTime ObterDataPresenca(List<Dictionary<string, string>> dadosPresenca)
        {
            DateTime? dataAula = null;
            int linhaId = 1;
            foreach (var linha in dadosPresenca)
            {
                linhaId++;
                string chaveData = linha.Keys.FirstOrDefault(k => k.Equals("data_presenca", StringComparison.OrdinalIgnoreCase));
                DateTime dataLinha;
                if (chaveData == null || !DateTime.TryParse(linha[chaveData]?.Trim(), out dataLinha))
                    throw new ArgumentException($"Erro na linha {linhaId}: Informe uma data válida na coluna 'data_presenca'.");

                if (dataAula.HasValue && dataLinha.Date != dataAula.Value)
                    throw new ArgumentException($"Erro na linha {linhaId}: Todas as datas de presença devem corresponder à mesma aula.");

                dataAula = dataLinha.Date;
            }
            return dataAula.Value;
        }

        internal static List<string> ValidarEObterAusentes(DateTime dataAula, HashSet<string> matriculasPresentes, List<Aluno> alunosDaTurma)
        {
            var alunosPorMatricula = alunosDaTurma.ToDictionary(a => a.Matricula);
            foreach (string matricula in matriculasPresentes)
            {
                Aluno aluno;
                if (!alunosPorMatricula.TryGetValue(matricula, out aluno))
                    throw new ArgumentException($"O aluno de matrícula {matricula} não está vinculado à turma selecionada.");

                if (!aluno.DataEntrada.HasValue)
                    throw new ArgumentException($"O aluno de matrícula {matricula} não possui data de entrada na turma. Atualize o cadastro antes de importar a presença.");

                if (dataAula.Date < aluno.DataEntrada.Value.Date)
                    throw new ArgumentException($"A data de presença ({dataAula:dd/MM/yyyy}) é anterior à entrada na turma do aluno de matrícula {matricula} ({aluno.DataEntrada:dd/MM/yyyy}).");
            }

            // O dia da entrada já pertence ao vínculo, como nas consultas de frequência.
            return alunosDaTurma
                .Where(a => a.DataEntrada.HasValue && a.DataEntrada.Value.Date <= dataAula.Date
                    && !matriculasPresentes.Contains(a.Matricula))
                .Select(a => a.Matricula)
                .ToList();
        }

        private string WebUtilityTrim(string valor)
        {
            return valor?.Trim() ?? "";
        }
    }
}
