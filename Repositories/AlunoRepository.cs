using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Text;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Relatorios;

namespace TCC_Assiduidade.Repositories
{
    public class AlunoRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public void Adicionar(Aluno aluno)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "o aluno");

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        TurmaRepository.VerificarTurmaAtiva(conn, trans, aluno.TurmaId);
                        string queryAluno = "INSERT INTO Aluno (Matricula, Nome, Email) VALUES (@matricula, @nome, @email)";
                        using (var cmd = new MySqlCommand(queryAluno, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                            cmd.Parameters.AddWithValue("@nome", aluno.Nome);
                            cmd.Parameters.AddWithValue("@email", aluno.Email ?? "");
                            cmd.ExecuteNonQuery();
                        }

                        if (aluno.TurmaId > 0)
                        {
                            string queryMatricula = "INSERT INTO VinculoTurmaAluno (AlunoMatricula, TurmaId, DataEntrada) VALUES (@alunoMatricula, @turmaId, @dataEntrada)";
                            using (var cmdMatricula = new MySqlCommand(queryMatricula, conn, trans))
                            {
                                cmdMatricula.Parameters.AddWithValue("@alunoMatricula", aluno.Matricula);
                                cmdMatricula.Parameters.AddWithValue("@turmaId", aluno.TurmaId);

                                // TRUQUE AQUI: Se a DataEntrada veio preenchida do ecrã, usa-a. Se não, usa o dia de hoje.
                                cmdMatricula.Parameters.AddWithValue("@dataEntrada", aluno.DataEntrada ?? DateTime.Now.Date);

                                cmdMatricula.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        trans.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "o aluno");
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Abre a conexão traduzindo qualquer falha (servidor fora do ar, sem rede,
        /// credenciais inválidas) em <see cref="ConexaoBancoDadosException"/>, com
        /// uma mensagem específica em vez do erro técnico bruto do driver MySQL.
        /// </summary>
        private static void AbrirConexao(MySqlConnection conn, string contexto)
        {
            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, contexto);
            }
        }

        public void Adicionar(List<Aluno> alunos)
        {
            // Se a lista estiver vazia, cancela a execução para evitar erros de sintaxe no SQL
            if (alunos == null || alunos.Count == 0) return;

            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "os alunos");

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (int turmaId in new HashSet<int>(alunos.ConvertAll(a => a.TurmaId)))
                            TurmaRepository.VerificarTurmaAtiva(conn, trans, turmaId);
                        StringBuilder sqlAlunos = new StringBuilder("INSERT INTO Aluno (Matricula, Nome, Email) VALUES ");
                        List<string> rowsAlunos = new List<string>();

                        StringBuilder sqlMatriculas = new StringBuilder("INSERT INTO VinculoTurmaAluno (AlunoMatricula, TurmaId, DataEntrada) VALUES ");
                        List<string> rowsMatriculas = new List<string>();

                        using (var cmd = new MySqlCommand())
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = trans;

                            for (int i = 0; i < alunos.Count; i++)
                            {
                                string mRef = "@m" + i;
                                string nRef = "@n" + i;
                                string eRef = "@e" + i;

                                rowsAlunos.Add($"({mRef}, {nRef}, {eRef})");

                                cmd.Parameters.AddWithValue(mRef, alunos[i].Matricula);
                                cmd.Parameters.AddWithValue(nRef, alunos[i].Nome);
                                cmd.Parameters.AddWithValue(eRef, alunos[i].Email ?? "");

                                if (alunos[i].TurmaId > 0)
                                {
                                    string amRef = "@am" + i;
                                    string tRef = "@t" + i;
                                    string dRef = "@d" + i;

                                    rowsMatriculas.Add($"({amRef}, {tRef}, {dRef})");

                                    cmd.Parameters.AddWithValue(amRef, alunos[i].Matricula);
                                    cmd.Parameters.AddWithValue(tRef, alunos[i].TurmaId);

                                    cmd.Parameters.AddWithValue(dRef, alunos[i].DataEntrada ?? DateTime.Now.Date);
                                }
                            }

                            sqlAlunos.Append(string.Join(",", rowsAlunos));
                            cmd.CommandText = sqlAlunos.ToString();
                            cmd.ExecuteNonQuery();

                            if (rowsMatriculas.Count > 0)
                            {
                                sqlMatriculas.Append(string.Join(",", rowsMatriculas));
                                cmd.CommandText = sqlMatriculas.ToString();
                                cmd.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        trans.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "os alunos");
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public int ContarMatriculasExistentes(List<string> matriculas)
        {
            if (matriculas == null || matriculas.Count == 0) return 0;

            List<string> parametros = new List<string>();
            for (int i = 0; i < matriculas.Count; i++)
            {
                parametros.Add($"@p{i}");
            }

            string sql = $"SELECT COUNT(1) FROM Aluno WHERE Matricula IN ({string.Join(", ", parametros)})";

            try
            {
                using (var conexao = new MySqlConnection(connectionString))
                using (var comando = new MySqlCommand(sql, conexao))
                {
                    for (int i = 0; i < matriculas.Count; i++)
                    {
                        comando.Parameters.AddWithValue($"@p{i}", matriculas[i]);
                    }

                    conexao.Open();

                    int totalExistente = Convert.ToInt32(comando.ExecuteScalar());

                    return totalExistente;
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os alunos");
            }
        }

        public int ContarAlunosDeOutraTurma(int turmaId, List<string> matriculas)
        {
            // Se a lista estiver vazia, nem precisamos ir ao banco de dados
            if (matriculas == null || matriculas.Count == 0) return 0;

            List<string> parametros = new List<string>();
            for (int i = 0; i < matriculas.Count; i++)
            {
                parametros.Add($"@p{i}");
            }

            // A query agora busca a matrícula ATUAL (a de maior data) de cada aluno 
            // e conta quantos deles estão vinculados a uma TurmaId diferente da informada
            string sql = $@"
                        SELECT COUNT(1) 
                        FROM VinculoTurmaAluno v
                        WHERE v.TurmaId <> @turmaId
                        AND v.AlunoMatricula IN ({string.Join(", ", parametros)})
                        AND v.DataEntrada = (
                            SELECT MAX(subM.DataEntrada)
                            FROM VinculoTurmaAluno subM
                            WHERE subM.AlunoMatricula = v.AlunoMatricula
                        )";

            try
            {
                using (var conexao = new MySqlConnection(connectionString))
                using (var comando = new MySqlCommand(sql, conexao))
                {
                    comando.Parameters.AddWithValue("@turmaId", turmaId);

                    for (int i = 0; i < matriculas.Count; i++)
                    {
                        comando.Parameters.AddWithValue($"@p{i}", matriculas[i]);
                    }

                    conexao.Open();
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os alunos");
            }
        }

        public List<Aluno> ObterPorTurma(int turmaId)
        {
            var alunos = new List<Aluno>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Filtra os alunos cuja maior DataEntrada na tabela Matricula seja igual à turma informada
                    string colunaExclusao = TurmaRepository.ObterColunaDataExclusao(conn, "t");
                    string query = $@"
                SELECT a.Matricula, a.Nome, a.Email, v.TurmaId, v.DataEntrada
                FROM Aluno a
                INNER JOIN VinculoTurmaAluno v ON a.Matricula = v.AlunoMatricula
                INNER JOIN Turma t ON t.Id = v.TurmaId
                WHERE v.TurmaId = @turmaId
                AND {colunaExclusao} IS NULL
                AND v.DataEntrada = (
                    SELECT MAX(subM.DataEntrada)
                    FROM VinculoTurmaAluno subM
                    WHERE subM.AlunoMatricula = a.Matricula
                )
                ORDER BY a.Nome ASC;";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@turmaId", turmaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                alunos.Add(new Aluno
                                {
                                    Matricula = reader["Matricula"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    TurmaId = Convert.ToInt32(reader["TurmaId"]),
                                    DataEntrada = reader["DataEntrada"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["DataEntrada"])
                                        : (DateTime?)null
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os alunos da turma");
            }

            return alunos;
        }

        public List<AlunoExibicaoDTO> ObterPerfilAluno()
        {
            var lista = new List<AlunoExibicaoDTO>();

            // Query ajustada para filtrar aulas e faltas a partir da DataEntrada do aluno
            const string consulta = @"
        SELECT 
            a.Matricula, 
            a.Nome, 
            a.Email, 
            v.DataEntrada,
            {0} AS DataExclusaoTurma,
            COALESCE(t.Nome, 'Sem turma') AS NomeTurma,
            
            -- Conta faltas apenas na turma atual e a partir da data de entrada
            COALESCE((
                SELECT COUNT(*) 
                FROM Ausencia aus 
                INNER JOIN Aula au_falta ON aus.AulaId = au_falta.Id
                WHERE aus.AlunoMatricula = a.Matricula 
                  AND au_falta.TurmaId = v.TurmaId
                  AND au_falta.Data >= v.DataEntrada
            ), 0) AS TotalFaltas,
            
            -- Conta aulas da turma atual criadas a partir da data de entrada do aluno
            COALESCE((
                SELECT COUNT(*) 
                FROM Aula au 
                WHERE au.TurmaId = v.TurmaId
                  AND au.Data >= v.DataEntrada
            ), 0) AS TotalAulas

        FROM Aluno a
        LEFT JOIN VinculoTurmaAluno v ON a.Matricula = v.AlunoMatricula AND v.DataEntrada = (
            SELECT MAX(subM.DataEntrada)
            FROM VinculoTurmaAluno subM
            WHERE subM.AlunoMatricula = a.Matricula
        )
        LEFT JOIN Turma t ON v.TurmaId = t.Id
        ORDER BY a.Nome ASC;";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = string.Format(consulta, TurmaRepository.ObterColunaDataExclusao(conn, "t"));
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int faltas = Convert.ToInt32(reader["TotalFaltas"]);
                            int totalAulas = Convert.ToInt32(reader["TotalAulas"]);

                            DateTime? dataEntradaValida = reader["DataEntrada"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DataEntrada"])
                                : (DateTime?)null;

                            var alunoDTO = new AlunoExibicaoDTO
                            {
                                Matricula = reader["Matricula"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                Turma = Turma.FormatarNome(reader["NomeTurma"].ToString(),
                                    reader["DataExclusaoTurma"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataExclusaoTurma"]) : null),
                                DataEntrada = dataEntradaValida,

                                DadosFrequencia = new RelatorioFrequencia
                                {
                                    TotalFaltas = faltas,
                                    TotalAulas = totalAulas
                                }
                            };

                            lista.Add(alunoDTO);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os dados dos alunos");
            }

            return lista;
        }

        public void Atualizar(Aluno aluno)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "o aluno");

                // Iniciamos uma transação para garantir a consistência das duas tabelas
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        TurmaRepository.VerificarTurmaAtiva(conn, transaction, aluno.TurmaId);
                        // 1. Atualiza os dados cadastrais do Aluno
                        string queryAluno = @"UPDATE Aluno
                                          SET Nome = @nome,
                                              Email = @email
                                          WHERE Matricula = @matricula;";

                        int linhasAluno;
                        using (var cmdAluno = new MySqlCommand(queryAluno, conn, transaction))
                        {
                            cmdAluno.Parameters.AddWithValue("@nome", aluno.Nome);
                            cmdAluno.Parameters.AddWithValue("@email", aluno.Email);
                            cmdAluno.Parameters.AddWithValue("@matricula", aluno.Matricula);
                            linhasAluno = cmdAluno.ExecuteNonQuery();
                        }

                        if (linhasAluno == 0)
                        {
                            string queryAlunoExiste = "SELECT COUNT(*) FROM Aluno WHERE Matricula = @matricula;";
                            using (var cmdAlunoExiste = new MySqlCommand(queryAlunoExiste, conn, transaction))
                            {
                                cmdAlunoExiste.Parameters.AddWithValue("@matricula", aluno.Matricula);
                                bool alunoExiste = Convert.ToInt32(cmdAlunoExiste.ExecuteScalar()) > 0;

                                if (!alunoExiste)
                                    throw new RegistroNaoEncontradoException($"O aluno de matrícula {aluno.Matricula} não foi encontrado. Ele pode já ter sido excluído por outro usuário.");
                            }
                        }

                        // O vínculo anterior permanece no histórico quando a turma muda.
                        int? turmaAnterior = null;
                        DateTime? entradaAnterior = null;
                        const string consultaVinculo = @"SELECT TurmaId, DataEntrada FROM VinculoTurmaAluno
                            WHERE AlunoMatricula = @matricula ORDER BY DataEntrada DESC LIMIT 1 FOR UPDATE";
                        using (var cmd = new MySqlCommand(consultaVinculo, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    turmaAnterior = Convert.ToInt32(reader["TurmaId"]);
                                    entradaAnterior = reader["DataEntrada"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(reader["DataEntrada"]) : null;
                                }
                            }
                        }

                        DateTime novaEntrada = (aluno.DataEntrada ?? DateTime.Today).Date;
                        bool novoVinculo = turmaAnterior != aluno.TurmaId;
                        if (novoVinculo && entradaAnterior.HasValue && novaEntrada <= entradaAnterior.Value.Date)
                            throw new ArgumentException("Ao mudar de turma, informe uma data de entrada posterior à do vínculo anterior para preservar o histórico.");

                        string queryVinculo = novoVinculo
                            ? "INSERT INTO VinculoTurmaAluno (AlunoMatricula, TurmaId, DataEntrada) VALUES (@matricula, @turmaId, @dataEntrada)"
                            : @"UPDATE VinculoTurmaAluno SET DataEntrada = @dataEntrada
                                WHERE AlunoMatricula = @matricula AND TurmaId = @turmaId
                                  AND DataEntrada <=> @entradaAnterior";

                        using (var cmdVinculo = new MySqlCommand(queryVinculo, conn, transaction))
                        {
                            cmdVinculo.Parameters.AddWithValue("@matricula", aluno.Matricula);
                            cmdVinculo.Parameters.AddWithValue("@dataEntrada", novaEntrada);
                            cmdVinculo.Parameters.AddWithValue("@turmaId", aluno.TurmaId);
                            if (!novoVinculo)
                                cmdVinculo.Parameters.AddWithValue("@entradaAnterior", (object)entradaAnterior ?? DBNull.Value);

                            cmdVinculo.ExecuteNonQuery();
                        }

                        // Se os dois UPDATEs deram certo, grava em definitivo no banco
                        transaction.Commit();
                    }
                    catch (RegistroNaoEncontradoException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                    catch (MySqlException ex)
                    {
                        transaction.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "o aluno");
                    }
                    catch
                    {
                        // Se algo falhar em qualquer uma das tabelas, desfaz tudo
                        transaction.Rollback();
                        throw; // Repassa o erro para o bloco catch externo
                    }
                }
            }
        }

        public void Excluir(string matricula)
        {
            Excluir(new List<string> { matricula });
        }

        public void Excluir(List<string> matriculas)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "os alunos");

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var parametros = matriculas
                            .Select((matricula, indice) => new
                            {
                                Nome = "@matricula" + indice,
                                Valor = matricula
                            })
                            .ToList();
                        string listaParametros = string.Join(", ", parametros.Select(p => p.Nome));

                        string queryAusencia = $"DELETE FROM Ausencia WHERE AlunoMatricula IN ({listaParametros});";
                        using (var cmd = new MySqlCommand(queryAusencia, conn, transaction))
                        {
                            foreach (var parametro in parametros)
                                cmd.Parameters.AddWithValue(parametro.Nome, parametro.Valor);
                            cmd.ExecuteNonQuery();
                        }

                        string queryVinculo = $"DELETE FROM VinculoTurmaAluno WHERE AlunoMatricula IN ({listaParametros});";
                        using (var cmd = new MySqlCommand(queryVinculo, conn, transaction))
                        {
                            foreach (var parametro in parametros)
                                cmd.Parameters.AddWithValue(parametro.Nome, parametro.Valor);
                            cmd.ExecuteNonQuery();
                        }

                        string queryAluno = $"DELETE FROM Aluno WHERE Matricula IN ({listaParametros});";
                        using (var cmd = new MySqlCommand(queryAluno, conn, transaction))
                        {
                            foreach (var parametro in parametros)
                                cmd.Parameters.AddWithValue(parametro.Nome, parametro.Valor);
                            int linhasAfetadas = cmd.ExecuteNonQuery();

                            if (linhasAfetadas != matriculas.Count)
                                throw new RegistroNaoEncontradoException("Um ou mais alunos selecionados não foram encontrados. Eles podem já ter sido excluídos por outro usuário.");
                        }

                        transaction.Commit();
                    }
                    catch (RegistroNaoEncontradoException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                    catch (MySqlException ex)
                    {
                        transaction.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "os alunos");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
