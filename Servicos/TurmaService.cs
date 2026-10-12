using System;
using System.Collections.Generic;
using System.Linq;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Repositories;

using TCC_Assiduidade.Excecoes;

namespace TCC_Assiduidade.Servicos
{
    public class TurmaService
    {
        private readonly TurmaRepository _turmaRepository;

        public TurmaService()
        {
            _turmaRepository = new TurmaRepository();
        }

        public void Adicionar(string turmaNome)
        {
            if (string.IsNullOrWhiteSpace(turmaNome))
                throw new ArgumentException("O nome da turma não pode ser nulo ou vazio.", nameof(turmaNome));

            var turmaExistente = _turmaRepository.ObterTurmaPorNome(turmaNome);
            if (turmaExistente != null)
                throw new RegistroDuplicadoException(turmaExistente.Status == EStatusTurma.Excluida
                    ? "Esse nome pertence a uma turma excluída cujo histórico foi preservado. Escolha outro nome para a nova turma."
                    : "Já existe uma turma com esse nome.");

            _turmaRepository.Adicionar(turmaNome);
        }

        public Turma ObterTurmaPorNome(string turmaNome)
        {
            if (string.IsNullOrWhiteSpace(turmaNome))
                return null;

            return _turmaRepository.ObterTurmaPorNome(turmaNome);
        }

        public List<Turma> ObterTodasTurmas()
        {
            return _turmaRepository.ObterTurmas();
        }

        public Turma ObterTurmaPorId(int turmaId)
        {
            if (turmaId <= 0)
                return null;

            return _turmaRepository.ObterTurmaPorId(turmaId);
        }

        public List<TurmaExibicaoDTO> ObterTurmasComContagem(bool incluirArquivadas = false)
        {
            return _turmaRepository.ObterTurmasComContagem(incluirArquivadas);
        }

        public void GarantirTurmaAtiva(int turmaId)
        {
            ValidarTurmaAtiva(ObterTurmaPorId(turmaId));
        }

        internal static void ValidarTurmaAtiva(Turma turma)
        {
            if (turma == null)
                throw new RegistroNaoEncontradoException("A turma especificada não existe.");
            if (turma.Status != EStatusTurma.Ativa)
                throw new ArgumentException(turma.Status == EStatusTurma.Arquivada
                    ? "A turma está arquivada e não aceita novas operações. Selecione uma turma ativa."
                    : "A turma foi excluída e não aceita novas operações. Selecione uma turma ativa.");
        }

        public void Atualizar(int turmaId, string novoNome)
        {
            if (turmaId <= 0)
                throw new ArgumentException("O ID da turma deve ser maior que zero.", nameof(turmaId));
            if (string.IsNullOrWhiteSpace(novoNome))
                throw new ArgumentException("O nome da turma não pode ser nulo ou vazio.", nameof(novoNome));
            var turmaExistente = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turmaExistente == null)
                throw new RegistroNaoEncontradoException("A turma especificada não existe.");
            ValidarTurmaAtiva(turmaExistente);
            var outraTurmaComMesmoNome = _turmaRepository.ObterTurmaPorNome(novoNome);
            if (outraTurmaComMesmoNome != null && outraTurmaComMesmoNome.Id != turmaId)
                throw new RegistroDuplicadoException("Já existe outra turma com esse nome.");
            _turmaRepository.Atualizar(turmaId, novoNome);
        }

        public void Excluir(int turmaId)
        {
            if (turmaId <= 0)
                throw new ArgumentException("O ID da turma deve ser maior que zero.", nameof(turmaId));
            var turmaExistente = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turmaExistente == null)
                throw new RegistroNaoEncontradoException("A turma especificada não existe.");
            if (turmaExistente.Status == EStatusTurma.Excluida)
                throw new ArgumentException("A turma já está excluída e não pode ser alterada novamente.");
            _turmaRepository.AlterarStatus(turmaId, turmaExistente.Status, EStatusTurma.Excluida);
        }

        public void Arquivar(int turmaId)
        {
            if (turmaId <= 0) throw new ArgumentException("O ID da turma deve ser maior que zero.", nameof(turmaId));
            Turma turma = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turma == null) throw new RegistroNaoEncontradoException("A turma especificada não existe.");
            ValidarTurmaAtiva(turma);
            _turmaRepository.AlterarStatus(turmaId, EStatusTurma.Ativa, EStatusTurma.Arquivada);
        }

        public void Desarquivar(int turmaId)
        {
            if (turmaId <= 0) throw new ArgumentException("O ID da turma deve ser maior que zero.");
            Turma turma = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turma == null) throw new RegistroNaoEncontradoException("A turma especificada não existe.");
            if (turma.Status != EStatusTurma.Arquivada)
                throw new ArgumentException("Somente turmas arquivadas podem ser desarquivadas.");
            _turmaRepository.AlterarStatus(turmaId, EStatusTurma.Arquivada, EStatusTurma.Ativa);
        }

        public void AlterarStatusEmLote(List<Tuple<int, EStatusTurma, EStatusTurma>> alteracoes)
        {
            if (alteracoes == null || alteracoes.Count == 0)
                throw new ArgumentException("Selecione pelo menos uma turma.");
            if (alteracoes.Any(a => a.Item1 <= 0 || a.Item2 == EStatusTurma.Excluida))
                throw new ArgumentException("A seleção contém turma inválida. Nenhuma turma foi alterada.");
            _turmaRepository.AlterarStatusEmLote(alteracoes);
        }
    }
}
