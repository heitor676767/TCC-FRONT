using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.Input;

namespace tccapp.ViewModels
{
    internal partial class UserCadastroViewModel : ObservableObject
    {


        [ObservableProperty]
        private List<string> generos = new() { "Masculino", "Feminino", "Prefiro não responder" };

        [ObservableProperty]
        private string generoSelecionado;

        // Propriedades calculadas automáticas para a View
        public string GeneroExibido => string.IsNullOrEmpty(GeneroSelecionado) ? "Selecione o Gênero" : GeneroSelecionado;
        // CORRIGIDO: Usando Color.FromHex para ler a string hexadecimal corretamente
        public Color GeneroTextColor => string.IsNullOrEmpty(GeneroSelecionado) ? Color.Parse("#999999") : Colors.Black;


        // Garante que o Label atualize na tela assim que o usuário clicar no Picker
        partial void OnGeneroSelecionadoChanged(string value)
        {
            OnPropertyChanged(nameof(GeneroExibido));
            OnPropertyChanged(nameof(GeneroTextColor));
        }

        [RelayCommand]
        private async Task VerificarEmail()
        {
            await Shell.Current.GoToAsync("Verificar");
        }
    }
    }
