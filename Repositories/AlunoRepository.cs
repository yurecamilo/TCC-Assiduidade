using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
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
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
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
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Adicionar(List<Aluno> alunos)
        {
            // Se a lista estiver vazia, cancela a execução para evitar erros de sintaxe no SQL
            if (alunos == null || alunos.Count == 0) return;

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
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

            using (var conexao = new MySqlConnection(connectionString))
            {
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

            using (var conexao = new MySqlConnection(connectionString))
            {
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
        }

        public List<Aluno> ObterPorTurma(int turmaId)
        {
            var alunos = new List<Aluno>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Filtra os alunos cuja maior DataEntrada na tabela Matricula seja igual à turma informada
                string query = @"
                SELECT a.Matricula, a.Nome, a.Email, v.TurmaId
                FROM Aluno a
                INNER JOIN VinculoTurmaAluno v ON a.Matricula = v.AlunoMatricula
                WHERE v.TurmaId = @turmaId
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
                                TurmaId = Convert.ToInt32(reader["TurmaId"])
                            });
                        }
                    }
                }
            }

            return alunos;
        }

        public List<AlunoExibicaoDTO> ObterPerfilAluno()
        {
            var lista = new List<AlunoExibicaoDTO>();

            // Query ajustada para filtrar aulas e faltas a partir da DataEntrada do aluno
            string sql = @"
        SELECT 
            a.Matricula, 
            a.Nome, 
            a.Email, 
            v.DataEntrada,
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

            using (var conn = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int faltas = Convert.ToInt32(reader["TotalFaltas"]);
                            int totalAulas = Convert.ToInt32(reader["TotalAulas"]);

                            double percentualAssiduidade = 100.0;
                            if (totalAulas > 0)
                            {
                                percentualAssiduidade = ((double)(totalAulas - faltas) / totalAulas) * 100;
                                percentualAssiduidade = Math.Round(percentualAssiduidade, 1);
                            }

                            DateTime? dataEntradaValida = reader["DataEntrada"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DataEntrada"])
                                : (DateTime?)null;

                            var alunoDTO = new AlunoExibicaoDTO
                            {
                                Matricula = reader["Matricula"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                Turma = reader["NomeTurma"].ToString(),
                                DataEntrada = dataEntradaValida,

                                DadosFrequencia = new RelatorioFrequencia
                                {
                                    TotalFaltas = faltas,
                                    TotalAulas = totalAulas,
                                    Assiduidade = percentualAssiduidade
                                }
                            };

                            lista.Add(alunoDTO);
                        }
                    }
                }
            }

            return lista;
        }

        public void Atualizar(Aluno aluno)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Iniciamos uma transação para garantir a consistência das duas tabelas
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Atualiza os dados cadastrais do Aluno
                            string queryAluno = @"UPDATE Aluno 
                                          SET Nome = @nome, 
                                              Email = @email 
                                          WHERE Matricula = @matricula;";

                            using (var cmdAluno = new MySqlCommand(queryAluno, conn, transaction))
                            {
                                cmdAluno.Parameters.AddWithValue("@nome", aluno.Nome);
                                cmdAluno.Parameters.AddWithValue("@email", aluno.Email);
                                cmdAluno.Parameters.AddWithValue("@matricula", aluno.Matricula);
                                cmdAluno.ExecuteNonQuery();
                            }

                            // 2. Atualiza o vínculo com a Turma e a Data de Entrada
                            string queryVinculo = @"UPDATE VinculoTurmaAluno 
                                            SET TurmaId = @turmaId, 
                                                DataEntrada = @dataEntrada 
                                            WHERE AlunoMatricula = @matricula;";

                            using (var cmdVinculo = new MySqlCommand(queryVinculo, conn, transaction))
                            {
                                cmdVinculo.Parameters.AddWithValue("@matricula", aluno.Matricula);
                                cmdVinculo.Parameters.AddWithValue("@dataEntrada", aluno.DataEntrada);

                                // Trata caso o aluno fique sem turma
                                cmdVinculo.Parameters.AddWithValue("@turmaId", (object)aluno.TurmaId ?? DBNull.Value);

                                cmdVinculo.ExecuteNonQuery();
                            }

                            // Se os dois UPDATEs deram certo, grava em definitivo no banco
                            transaction.Commit();
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Não foi possível atualizar os dados e o vínculo do aluno.", ex);
            }
        }

        public void Excluir(string matricula) 
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string queryAluno = @"
                    DELETE FROM Aluno WHERE Matricula = @matricula;";

                    using (var cmd = new MySqlCommand(queryAluno, conn))
                    {
                        cmd.Parameters.AddWithValue("@matricula", matricula);
                        cmd.ExecuteNonQuery();
                    }

                    string queryVinculo = @"
                    DELETE FROM VinculoTurmaAluno WHERE AlunoMatricula = @matricula;";

                    using (var cmd = new MySqlCommand(queryVinculo, conn))
                    {
                        cmd.Parameters.AddWithValue("@matricula", matricula);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Nao foi possivel excluir o aluno.", ex);
            }
        }
    }
}
