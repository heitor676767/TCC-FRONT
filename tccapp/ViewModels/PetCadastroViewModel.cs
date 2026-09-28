using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.ViewModels
{
    public partial class PetCadastroViewModel : ObservableObject
    {
        [ObservableProperty]
        private string porteSelecionado;

        [ObservableProperty]
        private string generoSelecionado;

        [ObservableProperty]
        private List<string> generos = new() { "Masculino", "Feminino"};

        [ObservableProperty]
        private List<string> porte = new() { "Grande", "Médio", "Pequeno" };
        public string PorteExibido => string.IsNullOrEmpty(PorteSelecionado) ? "Porte" : PorteSelecionado;
        public Color PorteTextColor => string.IsNullOrEmpty(PorteSelecionado) ? Color.Parse("#999999") : Colors.Black;
        public string GeneroExibido => string.IsNullOrEmpty(GeneroSelecionado) ? "Selecione o Gênero" : GeneroSelecionado;
        public Color GeneroTextColor => string.IsNullOrEmpty(GeneroSelecionado) ? Color.Parse("#999999") : Colors.Black;






        partial void OnPorteSelecionadoChanged(string value)
        {
            OnPropertyChanged(nameof(PorteExibido));
            OnPropertyChanged(nameof(PorteTextColor));
        }
        partial void OnGeneroSelecionadoChanged(string value)
        {
            OnPropertyChanged(nameof(GeneroExibido));
            OnPropertyChanged(nameof(GeneroTextColor));
        }
    }
}
