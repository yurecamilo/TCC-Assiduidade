using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Repositories;

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
                throw new InvalidOperationException("Já existe uma turma com esse nome.");

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

        public List<TurmaExibicaoDTO> ObterTurmasComContagem()
        {
            return _turmaRepository.ObterTurmasComContagem();
        }

        public void Atualizar(int turmaId, string novoNome)
        {
            if (turmaId <= 0)
                throw new ArgumentException("O ID da turma deve ser maior que zero.", nameof(turmaId));
            if (string.IsNullOrWhiteSpace(novoNome))
                throw new ArgumentException("O nome da turma não pode ser nulo ou vazio.", nameof(novoNome));
            var turmaExistente = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turmaExistente == null)
                throw new InvalidOperationException("A turma especificada não existe.");
            var outraTurmaComMesmoNome = _turmaRepository.ObterTurmaPorNome(novoNome);
            if (outraTurmaComMesmoNome != null && outraTurmaComMesmoNome.Id != turmaId)
                throw new InvalidOperationException("Já existe outra turma com esse nome.");
            _turmaRepository.Atualizar(turmaId, novoNome);
        }

        public void Excluir(int turmaId)
        {
            if (turmaId <= 0)
                throw new ArgumentException("O ID da turma deve ser maior que zero.", nameof(turmaId));
            var turmaExistente = _turmaRepository.ObterTurmaPorId(turmaId);
            if (turmaExistente == null)
                throw new InvalidOperationException("A turma especificada não existe.");
            _turmaRepository.Excluir(turmaId);
        }
    }
}
