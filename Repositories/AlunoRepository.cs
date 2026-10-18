using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Linq;
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
                        BloquearMatricula(conn, trans, aluno.Matricula);
                        bool alunoExiste = ExisteAluno(conn, trans, aluno.Matricula);
                        if (alunoExiste)
                        {
                            ValidarMesmoAluno(conn, trans, aluno);
                            VerificarSemVinculoAtivo(conn, trans, aluno.Matricula);
                            VerificarDataNovoVinculo(conn, trans, aluno.Matricula, aluno.DataEntrada ?? DateTime.Today);
                        }
                        if (!alunoExiste)
                        {
                            using (var cmd = new MySqlCommand("INSERT INTO Aluno (Matricula, Nome, Email) VALUES (@matricula, @nome, @email)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                                cmd.Parameters.AddWithValue("@nome", aluno.Nome);
                                cmd.Parameters.AddWithValue("@email", aluno.Email ?? "");
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (aluno.TurmaId > 0 && aluno.DataEntrada.HasValue)
                            VerificarDataNovoVinculo(conn, trans, aluno.Matricula, aluno.DataEntrada.Value);

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
                        var matriculasNovas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (Aluno aluno in alunos)
                        {
                            BloquearMatricula(conn, trans, aluno.Matricula);
                            if (!matriculasNovas.Add(aluno.Matricula))
                                throw new ArgumentException("O arquivo contém matrícula repetida: " + aluno.Matricula + ". Nenhum aluno foi importado.");
                            if (ExisteAluno(conn, trans, aluno.Matricula))
                                throw new RegistroDuplicadoException("A matrícula " + aluno.Matricula + " já existe no cadastro. Nenhum aluno foi importado.");
                        }
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

        public void ImportarCadastroAtomico(string turmaNome, List<Aluno> alunos, bool criarTurma)
        {
            if (alunos == null || alunos.Count == 0) throw new ArgumentException("A importação não contém alunos.");
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a importação de alunos");
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int turmaId;
                        using (var cmd = new MySqlCommand("SELECT Id FROM Turma WHERE Nome = @nome LIMIT 1 FOR UPDATE", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@nome", turmaNome);
                            object encontrado = cmd.ExecuteScalar();
                            if (encontrado == null)
                            {
                                if (!criarTurma) throw new RegistroNaoEncontradoException("A turma de destino não existe.");
                                using (var inserir = new MySqlCommand("INSERT INTO Turma (Nome) VALUES (@nome); SELECT LAST_INSERT_ID();", conn, tx))
                                {
                                    inserir.Parameters.AddWithValue("@nome", turmaNome);
                                    turmaId = Convert.ToInt32(inserir.ExecuteScalar());
                                }
                            }
                            else turmaId = Convert.ToInt32(encontrado);
                        }

                        TurmaRepository.VerificarTurmaAtiva(conn, tx, turmaId);
                        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var aluno in alunos)
                        {
                            BloquearMatricula(conn, tx, aluno.Matricula);
                            if (!vistos.Add(aluno.Matricula)) throw new RegistroDuplicadoException("Matrícula repetida no arquivo: " + aluno.Matricula + ". Nenhum dado foi importado.");
                            bool existe = ExisteAluno(conn, tx, aluno.Matricula);
                            if (existe)
                            {
                                ValidarMesmoAluno(conn, tx, aluno);
                                VerificarSemVinculoAtivo(conn, tx, aluno.Matricula);
                                VerificarDataNovoVinculo(conn, tx, aluno.Matricula, aluno.DataEntrada ?? DateTime.Today);
                            }
                            else if (aluno.DataEntrada.HasValue)
                            {
                                VerificarDataNovoVinculo(conn, tx, aluno.Matricula, aluno.DataEntrada.Value);
                            }
                            aluno.TurmaId = turmaId;
                        }

                        StringBuilder vinculosSql = new StringBuilder("INSERT INTO VinculoTurmaAluno (AlunoMatricula, TurmaId, DataEntrada) VALUES ");
                        var vinculoValues = new List<string>();
                        using (var cmd = new MySqlCommand { Connection = conn, Transaction = tx })
                        {
                            for (int i = 0; i < alunos.Count; i++)
                            {
                                var a = alunos[i];
                                string m = "@m" + i, n = "@n" + i, e = "@e" + i, t = "@t" + i, d = "@d" + i;
                                vinculoValues.Add("(" + m + "," + t + "," + d + ")");
                                cmd.Parameters.AddWithValue(m, a.Matricula); cmd.Parameters.AddWithValue(n, a.Nome);
                                cmd.Parameters.AddWithValue(e, a.Email ?? ""); cmd.Parameters.AddWithValue(t, turmaId);
                                cmd.Parameters.AddWithValue(d, a.DataEntrada ?? DateTime.Today);
                                if (!ExisteAluno(conn, tx, a.Matricula))
                                {
                                    cmd.CommandText = "INSERT INTO Aluno (Matricula, Nome, Email) VALUES (" + m + "," + n + "," + e + ")";
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            cmd.CommandText = vinculosSql.Append(string.Join(",", vinculoValues)).ToString(); cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch (MySqlException ex) { tx.Rollback(); throw MySqlExceptionTranslator.Traduzir(ex, "a importação de alunos"); }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public int ImportarAulaEAusenciasAtomico(DateTime data, int turmaId, List<string> presentes)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a importação de presença");
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        TurmaRepository.VerificarTurmaAtiva(conn, tx, turmaId);
                        var matriculasTurma = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        const string sqlTurma = @"SELECT v.AlunoMatricula
                            FROM VinculoTurmaAluno v
                            INNER JOIN Turma t ON t.Id = v.TurmaId AND t.Status = 1
                            WHERE v.TurmaId = @turma AND v.DataEntrada <= @data
                              AND v.DataEntrada = (
                                  SELECT MAX(v2.DataEntrada) FROM VinculoTurmaAluno v2
                                  WHERE v2.AlunoMatricula = v.AlunoMatricula AND v2.DataEntrada <= @data
                              ) FOR UPDATE";
                        using (var cmd = new MySqlCommand(sqlTurma, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@turma", turmaId); cmd.Parameters.AddWithValue("@data", data.Date);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read()) matriculasTurma.Add(reader.GetString(0));
                            }
                        }
                        var presentesSet = new HashSet<string>(presentes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        string foraDaTurma = presentesSet.FirstOrDefault(m => !matriculasTurma.Contains(m));
                        if (foraDaTurma != null) throw new ArgumentException("O aluno de matrícula " + foraDaTurma + " não pertence à turma selecionada na data da aula. Nenhum dado foi importado.");
                        var ausentes = matriculasTurma.Where(m => !presentesSet.Contains(m)).ToList();
                        if (matriculasTurma.Count == 0) throw new ArgumentException("Não há alunos vinculados à turma na data informada. Nenhum dado foi importado.");
                        int aulaId;
                        using (var cmd = new MySqlCommand("INSERT INTO Aula (Data, TurmaId) VALUES (@data, @turma); SELECT LAST_INSERT_ID();", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@data", data.Date); cmd.Parameters.AddWithValue("@turma", turmaId);
                            aulaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        if (ausentes != null && ausentes.Count > 0)
                        {
                            var sql = new StringBuilder("INSERT INTO Ausencia (AulaId, AlunoMatricula) VALUES ");
                            var values = new List<string>();
                            using (var cmd = new MySqlCommand { Connection = conn, Transaction = tx })
                            {
                                for (int i = 0; i < ausentes.Count; i++)
                                {
                                    string a = "@a" + i, m = "@m" + i; values.Add("(" + a + "," + m + ")");
                                    cmd.Parameters.AddWithValue(a, aulaId); cmd.Parameters.AddWithValue(m, ausentes[i]);
                                }
                                cmd.CommandText = sql.Append(string.Join(",", values)).ToString(); cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit(); return aulaId;
                    }
                    catch (MySqlException ex) { tx.Rollback(); throw MySqlExceptionTranslator.Traduzir(ex, "a importação de presença"); }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        private static bool ExisteAluno(MySqlConnection conn, MySqlTransaction trans, string matricula)
        {
            using (var cmd = new MySqlCommand("SELECT Matricula FROM Aluno WHERE Matricula = @matricula FOR UPDATE", conn, trans))
            {
                cmd.Parameters.AddWithValue("@matricula", matricula);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void ValidarMesmoAluno(MySqlConnection conn, MySqlTransaction trans, Aluno aluno)
        {
            const string sql = "SELECT Nome FROM Aluno WHERE Matricula = @matricula FOR UPDATE";
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                string nomeCadastrado = Convert.ToString(cmd.ExecuteScalar());
                if (!string.Equals(nomeCadastrado?.Trim(), aluno.Nome?.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new RegistroDuplicadoException("A matrícula " + aluno.Matricula + " já pertence a outro aluno. Confira a matrícula e o nome; nenhum dado foi alterado.");
            }
        }

        // Serializa operações para a mesma matrícula, inclusive o primeiro cadastro
        // quando ainda não há linha em Aluno. A linha pai Turma é bloqueada enquanto
        // o lote avalia e grava os vínculos para evitar duas associações ativas.
        private static void BloquearMatricula(MySqlConnection conn, MySqlTransaction trans, string matricula)
        {
            using (var cmd = new MySqlCommand("SELECT Matricula FROM Aluno ORDER BY Matricula FOR UPDATE", conn, trans))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) { }
        }

        private static void VerificarSemVinculoAtivo(MySqlConnection conn, MySqlTransaction trans, string matricula)
        {
            const string sql = @"SELECT t.Id FROM VinculoTurmaAluno v
                INNER JOIN Turma t ON t.Id = v.TurmaId
                WHERE v.AlunoMatricula = @matricula
                  AND t.Status = 1 LIMIT 1 FOR UPDATE";
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@matricula", matricula);
                if (cmd.ExecuteScalar() != null)
                    throw new RegistroDuplicadoException("O aluno " + matricula + " já está associado a uma turma ativa. Nenhum dado foi alterado.");
            }
        }

        private static void VerificarDataNovoVinculo(MySqlConnection conn, MySqlTransaction trans, string matricula, DateTime novaEntrada)
        {
            const string sql = @"SELECT MAX(v.DataEntrada)
                FROM VinculoTurmaAluno v
                INNER JOIN Turma t ON t.Id = v.TurmaId
                WHERE v.AlunoMatricula = @matricula AND t.Status <> 3";
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@matricula", matricula);
                object valor = cmd.ExecuteScalar();
                if (valor != null && valor != DBNull.Value && novaEntrada.Date <= Convert.ToDateTime(valor).Date)
                    throw new ArgumentException("A data de entrada deve ser posterior ao vínculo anterior para manter o histórico do aluno. Nenhum dado foi alterado.");
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

        public bool ExisteVinculoAtivo(string matricula)
        {
            const string sql = @"SELECT COUNT(*) FROM VinculoTurmaAluno v
                INNER JOIN Turma t ON t.Id = v.TurmaId
                WHERE v.AlunoMatricula = @matricula AND t.Status = 1";
            using (var conn = new MySqlConnection(connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@matricula", matricula);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
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

                    // O resumo da turma também apresenta o histórico arquivado.
                    // A contagem da turma considera todos os vínculos registrados nela.
                    string query = $@"
                SELECT a.Matricula, a.Nome, a.Email, v.TurmaId, v.DataEntrada
                FROM Aluno a
                INNER JOIN VinculoTurmaAluno v ON a.Matricula = v.AlunoMatricula
                INNER JOIN Turma t ON t.Id = v.TurmaId
                WHERE v.TurmaId = @turmaId
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

        public List<AlunoExibicaoDTO> ObterPerfilAluno(bool incluirArquivados = false)
        {
            var lista = new List<AlunoExibicaoDTO>();

            // Query ajustada para filtrar aulas e faltas a partir da DataEntrada do aluno
            const string consulta = @"
        SELECT 
            a.Matricula, 
            a.Nome, 
            a.Email, 
            v.DataEntrada,
            {0} AS StatusTurma,
            {1} AS DataAlteracaoStatus,
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
        LEFT JOIN VinculoTurmaAluno v ON a.Matricula = v.AlunoMatricula AND (
            (incluir_arquivadas = 1 AND v.DataEntrada = (
                SELECT MAX(subM.DataEntrada) FROM VinculoTurmaAluno subM
                WHERE subM.AlunoMatricula = a.Matricula AND subM.TurmaId = v.TurmaId
            )) OR
            (incluir_arquivadas = 0 AND v.DataEntrada = (
                SELECT MAX(subM.DataEntrada) FROM VinculoTurmaAluno subM
                INNER JOIN Turma turmaAtual ON turmaAtual.Id = subM.TurmaId
                WHERE subM.AlunoMatricula = a.Matricula AND turmaAtual.Status = 1
            ))
        )
        LEFT JOIN Turma t ON v.TurmaId = t.Id
        WHERE (t.Status = 1 OR (incluir_arquivadas = 1 AND t.Status = 2))
        ORDER BY a.Nome ASC;";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = string.Format(consulta, TurmaRepository.ObterColunaStatus(conn, "t"), TurmaRepository.ObterColunaDataStatus(conn, "t"))
                        .Replace("incluir_arquivadas = 1", (incluirArquivados ? "1" : "0") + " = 1")
                        .Replace("incluir_arquivadas = 0", (incluirArquivados ? "1" : "0") + " = 0");
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
                                Turma = Turma.FormatarNome(reader["NomeTurma"].ToString(), reader["NomeTurma"].ToString() == "Sem turma" ? EStatusTurma.Ativa : (EStatusTurma)Convert.ToInt32(reader["StatusTurma"])),
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
                            throw new RegistroNaoEncontradoException($"O aluno de matrícula {aluno.Matricula} não foi encontrado. Ele pode já ter sido excluído por outro usuário.");

                        // Edição de dados pessoais não altera vínculos nem histórico.
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
            if (matriculas == null || matriculas.Count == 0) return;
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "o aluno");
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var unicas = matriculas.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        if (unicas.Count != matriculas.Count)
                            throw new ArgumentException("A seleção contém matrícula inválida ou repetida. Nenhum aluno foi excluído.");

                        foreach (string matricula in unicas)
                        {
                            using (var cmd = new MySqlCommand("SELECT Matricula FROM Aluno WHERE Matricula = @matricula FOR UPDATE", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@matricula", matricula);
                                if (cmd.ExecuteScalar() == null)
                                    throw new RegistroNaoEncontradoException($"O aluno {matricula} não foi encontrado. Nenhum aluno foi excluído.");
                            }
                        }

                        foreach (string matricula in unicas)
                        {
                            using (var cmd = new MySqlCommand("DELETE FROM Ausencia WHERE AlunoMatricula = @matricula", conn, transaction))
                            { cmd.Parameters.AddWithValue("@matricula", matricula); cmd.ExecuteNonQuery(); }
                            using (var cmd = new MySqlCommand("DELETE FROM VinculoTurmaAluno WHERE AlunoMatricula = @matricula", conn, transaction))
                            { cmd.Parameters.AddWithValue("@matricula", matricula); cmd.ExecuteNonQuery(); }
                            using (var cmd = new MySqlCommand("DELETE FROM Aluno WHERE Matricula = @matricula", conn, transaction))
                            { cmd.Parameters.AddWithValue("@matricula", matricula); cmd.ExecuteNonQuery(); }

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
                        throw MySqlExceptionTranslator.Traduzir(ex, "o aluno");
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
