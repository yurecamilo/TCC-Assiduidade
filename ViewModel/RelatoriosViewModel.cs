using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.View;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{

    public class RelatoriosViewModel : BaseViewModel
    {
        private object _conteudoAtual;
        public object ConteudoAtual
        {
            get => _conteudoAtual;
            set
            {
                _conteudoAtual = value;
                OnPropertyChanged(nameof(ConteudoAtual)); // <--- ISTO É OBRIGATÓRIO
            }
        }

        // Exemplo de comando do botão
        public ICommand AbrirRelatorioAulasCommand => new RelayCommand(() => {
            // Ao trocar a ViewModel, o DataTemplate do WPF substitui a View automaticamente
            ConteudoAtual = new RelatorioAulasView(this);
        });

        public ICommand AbrirRelatorioAlunosCommand => new RelayCommand(() =>
        {
            ConteudoAtual = new RelatorioAlunosView(this);
        });
    }
}