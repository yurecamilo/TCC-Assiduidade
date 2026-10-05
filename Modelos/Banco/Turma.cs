namespace TCC_Assiduidade.Modelos.Banco
{
    public class Turma
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public System.DateTime? DataExclusao { get; set; }
        public string NomeExibicao => FormatarNome(Nome, DataExclusao);

        internal static string FormatarNome(string nome, System.DateTime? dataExclusao)
        {
            return dataExclusao.HasValue ? $"{nome} (turma excluída)" : nome;
        }
    }
}
