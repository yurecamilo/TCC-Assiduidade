using System;

namespace TCC_Assiduidade.Excecoes
{
    /// <summary>
    /// Lançada quando uma operação tentaria gravar um registro que já existe
    /// (violação de restrição de unicidade, ex.: matrícula de aluno ou nome de
    /// turma repetidos).
    /// </summary>
    public class RegistroDuplicadoException : Exception
    {
        public RegistroDuplicadoException(string mensagem) : base(mensagem) { }

        public RegistroDuplicadoException(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna) { }
    }
}
