using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace TCC_Assiduidade
{
    public partial class MainWindow : Window
    {
        string connectionString = "server=switchyard.proxy.rlwy.net;database=railway;user=root;password=ACaRpVAAgmoyXtiEvdYlHtLTISAzUSZS;port=26278";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV da turma"
            };

            if (dialog.ShowDialog() == true)
            {
                LerCsv(dialog.FileName);
            }
        }

        private void LerCsv(string caminho)
        {
            try
            {
                using (var reader = new StreamReader(caminho, Encoding.UTF8))
                {
                    string linhaTurma = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(linhaTurma))
                    {
                        MessageBox.Show("Arquivo vazio");
                        return;
                    }

                    string[] partesTurma = linhaTurma.Split(';');
                    var turma = new Turma();

                    foreach (string parte in partesTurma)
                    {
                        string[] kv = parte.Split(':');
                        if (kv.Length == 2)
                        {
                            string chave = kv[0].Trim();
                            string valor = kv[1].Trim();

                            if (chave.Contains("Título") || chave.Contains("Titulo"))
                                turma.Titulo = valor;
                            else if (chave.Contains("Período") || chave.Contains("Periodo"))
                                turma.Periodo = valor;
                        }
                    }

                    int turmaId = CadastrarTurma(turma);
                    if (turmaId == 0)
                    {
                        MessageBox.Show("Erro ao cadastrar a turma.");
                        return;
                    }

                    tbDados.AppendText($"Turma: {turma.Titulo} | Período: {turma.Periodo} | ID: {turmaId}\n\n");

                    string headerLine = reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        string linha = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(linha))
                            continue;

                        string[] valores = linha.Split(';');
                        if (valores.Length < 2)
                            continue;

                        var aluno = new Aluno
                        {
                            Matricula = valores[0].Trim(),
                            Nome = valores[1].Trim(),
                            TurmaId = turmaId
                        };

                        if (!string.IsNullOrWhiteSpace(aluno.Matricula))
                        {
                            if (SalvarAluno(aluno))
                                tbDados.AppendText($"Aluno {aluno.Nome} salvo com sucesso.\n");
                            else
                                tbDados.AppendText($"Erro ao salvar aluno {aluno.Nome}.\n");
                        }
                    }
                }

                MessageBox.Show("CSV processado com sucesso!");
                CarregarAlunosDoBanco();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ler CSV: " + ex.Message);
            }
        }

        public int CadastrarTurma(Turma turma)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "INSERT INTO turmas (titulo, periodo) VALUES (@titulo, @periodo); SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@titulo", turma.Titulo);
                        cmd.Parameters.AddWithValue("@periodo", turma.Periodo);

                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        public bool SalvarAluno(Aluno aluno)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "INSERT INTO alunos (matricula, nome) VALUES (@matricula, @nome)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                        cmd.Parameters.AddWithValue("@nome", aluno.Nome);

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

                string query = "SELECT matricula, nome FROM alunos";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        alunos.Add(new Aluno
                        {
                            Matricula = reader["matricula"].ToString(),
                            Nome = reader["nome"].ToString()
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
    }
}
