using System;
using MySql.Data.MySqlClient;
using TCC_Assiduidade.Excecoes;

namespace TCC_Assiduidade.Repositories
{
    /// <summary>
    /// Traduz erros levantados pelo driver do MySQL (identificados pelo número do
    /// erro) em exceções de domínio com mensagens específicas para cada tipo de
    /// problema — em vez de uma única mensagem genérica ("não foi possível
    /// concluir a operação") para qualquer falha de banco de dados.
    ///
    /// Os repositórios devem capturar <see cref="MySqlException"/> e repassar para
    /// <see cref="Traduzir"/>, informando um contexto (ex.: "o aluno", "a turma")
    /// que é incorporado na mensagem final mostrada ao usuário.
    /// </summary>
    public static class MySqlExceptionTranslator
    {
        // Códigos de erro do MySQL (ver mysqld_error.h / errmsg.h)
        private const int ErroEntradaDuplicada = 1062;              // ER_DUP_ENTRY
        private const int ErroLinhaReferenciadaPai = 1451;          // ER_ROW_IS_REFERENCED_2 (exclusão bloqueada por FK)
        private const int ErroLinhaReferenciadaFilho = 1452;        // ER_NO_REFERENCED_ROW_2 (inserção com FK inválida)
        private const int ErroAcessoNegado = 1045;                  // ER_ACCESS_DENIED_ERROR
        private const int ErroBancoDesconhecido = 1049;             // ER_BAD_DB_ERROR
        private const int ErroMuitasConexoes = 1040;                // ER_CON_COUNT_ERROR
        private const int ErroConexaoSocket = 2002;                 // CR_CONNECTION_ERROR
        private const int ErroConexaoServidor = 2003;                // CR_CONN_HOST_ERROR
        private const int ErroServidorIndisponivel = 2006;           // CR_SERVER_GONE_ERROR
        private const int ErroConexaoPerdida = 2013;                 // CR_SERVER_LOST
        private const int ErroHostDesconhecido = 2005;               // CR_UNKNOWN_HOST
        private const int ErroConexaoPerdidaEstendida = 2055;        // CR_SERVER_LOST_EXTENDED

        public static Exception Traduzir(MySqlException excecao, string contexto)
        {
            if (excecao == null)
                throw new ArgumentNullException(nameof(excecao));

            return TraduzirPorNumero(excecao.Number, excecao.Message, contexto, excecao);
        }

        public static Exception TraduzirSeNecessario(Exception excecao, string contexto)
        {
            if (excecao == null)
                throw new ArgumentNullException(nameof(excecao));

            var erroMySql = excecao as MySqlException;
            return erroMySql == null ? excecao : Traduzir(erroMySql, contexto);
        }

        /// <summary>
        /// Núcleo testável da tradução: recebe apenas o número do erro (como o MySQL
        /// o reporta) e o contexto textual, sem depender de uma instância real de
        /// <see cref="MySqlException"/>. Isso permite cobrir cada tipo de erro em
        /// testes automatizados sem precisar de um banco de dados de verdade.
        /// </summary>
        public static Exception TraduzirPorNumero(int numeroErro, string mensagemOriginal, string contexto, Exception excecaoInterna = null)
        {
            string alvo = string.IsNullOrWhiteSpace(contexto) ? "o registro" : contexto;

            switch (numeroErro)
            {
                case ErroEntradaDuplicada:
                    return new RegistroDuplicadoException(
                        $"Já existe {alvo} cadastrado com esses dados. Verifique se não há duplicidade (matrícula ou nome já utilizados) e tente novamente.",
                        excecaoInterna);

                case ErroLinhaReferenciadaPai:
                    return new IntegridadeReferencialException(
                        $"Não é possível concluir esta operação em {alvo} porque existem outros registros vinculados a ele(a) no sistema.",
                        excecaoInterna);

                case ErroLinhaReferenciadaFilho:
                    return new IntegridadeReferencialException(
                        $"Não é possível concluir esta operação em {alvo} porque um dos registros relacionados não existe mais no sistema.",
                        excecaoInterna);

                case ErroAcessoNegado:
                case ErroBancoDesconhecido:
                case ErroMuitasConexoes:
                case ErroConexaoSocket:
                case ErroConexaoServidor:
                case ErroServidorIndisponivel:
                case ErroConexaoPerdida:
                case ErroHostDesconhecido:
                case ErroConexaoPerdidaEstendida:
                    return new ConexaoBancoDadosException(
                        "Não foi possível conectar ao banco de dados. Verifique sua conexão com a internet e tente novamente em instantes.",
                        excecaoInterna);

                default:
                    return new InvalidOperationException(
                        $"Ocorreu um erro inesperado ao processar {alvo}. Detalhes técnicos: {mensagemOriginal}",
                        excecaoInterna);
            }
        }
    }
}
