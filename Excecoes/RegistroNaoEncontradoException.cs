using System;

namespace TCC_Assiduidade.Excecoes
{
    /// <summary>
    /// Lançada quando uma operação (atualizar, excluir) é feita sobre um registro
    /// que não existe mais no banco de dados — por exemplo, porque outro usuário
    /// já o excluiu.
    /// </summary>
    public class RegistroNaoEncontradoException : Exception
    {
        public RegistroNaoEncontradoException(string mensagem) : base(mensagem) { }

        public RegistroNaoEncontradoException(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna) { }
    }
}
