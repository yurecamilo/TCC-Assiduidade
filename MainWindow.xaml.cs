using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Windows;

namespace TCC_Assiduidade
{
    public partial class MainWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;
        public MainWindow()
        {
            InitializeComponent();
            tbDados.Clear();
            tbTurmaId.Clear();
            tbTurmaNome.Clear();
        }
        private void Importacao(Turma turma, List<Dictionary<string, string>> dados)
        {
            if (turma == null)
            {
                tbDados.AppendText("Erro: Dados da turma não fornecidos. Por favor, preencha os campos de ID e Nome da turma.\n");
                return;
            }

            if (!TurmaExiste(turma.Id))
            {
                if (SalvarTurma(turma))
                {
                    tbDados.AppendText($"Turma {turma.Nome} (ID: {turma.Id}) salva com sucesso.\n");
                }
                else
                {
                    tbDados.AppendText($"Erro ao salvar turma {turma.Nome} (ID: {turma.Id}). Verifique os dados da turma.\n");
                }
            }

            List<Aluno> alunosParaSalvar = new List<Aluno>();

            foreach (var linha in dados)
            {
                var aluno = new Aluno
                {
                    Matricula = linha["matricula"],
                    Nome = linha["nome"],
                    TurmaId = turma.Id
                };

                alunosParaSalvar.Add(aluno);
            }
            tbDados.Clear();
            if (SalvarAlunos(alunosParaSalvar))
            {
                tbDados.AppendText($"Importação realizada com sucesso!\n");
                foreach (var a in alunosParaSalvar) tbDados.AppendText($"- {a.Nome} (Matrícula: {a.Matricula})\n");
            }
            else
            {
                tbDados.AppendText($"Erro ao salvar alunos. Verifique se a turma existe.\n");
            }
            
        }
        private void buttonImportarCadastro_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV da turma"
            };
            int TurmaId = int.Parse(tbTurmaId.Text);
            string TurmaNome = tbTurmaNome.Text;
            var turma = new Turma
            {
                Id = TurmaId,
                Nome = TurmaNome
            };
            if (dialog.ShowDialog() == true)
            {
                var dados = CsvUtils.LerCsv(dialog.FileName);
                Importacao(turma, dados);
            }
        }

        private bool SalvarAlunos(List<Aluno> listaAlunos)
        {
            if (listaAlunos == null || listaAlunos.Count == 0) return false;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Iniciamos a construção do comando em massa
                    StringBuilder sql = new StringBuilder("INSERT INTO Aluno (Matricula, Nome, TurmaId) VALUES ");
                    List<string> rows = new List<string>();

                    using (var cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;

                        for (int i = 0; i < listaAlunos.Count; i++)
                        {
                            // Criamos nomes únicos para os parâmetros para evitar conflitos
                            string mRef = "@m" + i;
                            string nRef = "@n" + i;
                            string tRef = "@t" + i;

                            rows.Add($"({mRef}, {nRef}, {tRef})");

                            cmd.Parameters.AddWithValue(mRef, listaAlunos[i].Matricula);
                            cmd.Parameters.AddWithValue(nRef, listaAlunos[i].Nome);
                            cmd.Parameters.AddWithValue(tRef, listaAlunos[i].TurmaId);
                        }

                        // Junta todas as linhas separadas por vírgula
                        sql.Append(string.Join(",", rows));

                        // Adicionamos a lógica para atualizar caso a matrícula já exista
                        sql.Append(" ON DUPLICATE KEY UPDATE Nome = VALUES(Nome), TurmaId = VALUES(TurmaId)");

                        cmd.CommandText = sql.ToString();
                        cmd.ExecuteNonQuery();
                    }
                }

                // Se chegou aqui, deu tudo certo
                return true;
            }
            catch (Exception ex)
            {
                // Aqui você pode logar o erro para saber o que falhou (ex: SQL inválido, queda de conexão)
                MessageBox.Show("Erro na importação em massa: " + ex.Message);
                return false;
            }
        }

        // Método para salvar aluno no banco de dados
        public bool SalvarAluno(Aluno aluno)
        {
            try
            {

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "INSERT INTO Aluno (Matricula, Nome, TurmaId) VALUES (@matricula, @nome, @turmaId)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                        cmd.Parameters.AddWithValue("@nome", aluno.Nome);
                        if (TurmaExiste(aluno.TurmaId))
                        {
                            //MessageBox.Show($"Turma com ID {aluno.TurmaId} existe. Associando aluno à turma.");
                            cmd.Parameters.AddWithValue("@turmaId", aluno.TurmaId);
                        }
                        else
                        {
                            //MessageBox.Show($"Turma com ID {aluno.TurmaId} não existe. Por favor, cadastre a turma antes de associar alunos.");
                            return false;
                        }
                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        //Método para salvar turma no banco de dados
        public bool SalvarTurma(Turma turma)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO Turma (Id, Nome) VALUES (@id, @nome)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", turma.Id);
                        cmd.Parameters.AddWithValue("@nome", turma.Nome);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<Aluno> CarregarAlunosDoBanco()
        {
            var alunos = new List<Aluno>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT Matricula, Nome FROM Aluno";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        alunos.Add(new Aluno
                        {
                            Matricula = reader["Matricula"].ToString(),
                            Nome = reader["Nome"].ToString()
                        });
                    }
                }
            }

            tbDados.AppendText("\n--- Alunos no banco ---\n");
            foreach (var a in alunos)
            {
                tbDados.AppendText($"Matrícula: {a.Matricula} | Nome: {a.Nome}\n");
            }

            return alunos;
        }
        public bool TurmaExiste(int turmaId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM Turma WHERE Id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", turmaId);
                        var result = Convert.ToInt32(cmd.ExecuteScalar());
                        return result > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void buttonAbrirTelaPresenca_Click(object sender, RoutedEventArgs e)
        {
            TelaPresenca tela = new TelaPresenca();
            tela.Show();
        }
    }
}


