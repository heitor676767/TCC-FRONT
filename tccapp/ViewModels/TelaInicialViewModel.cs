using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace tccapp.ViewModels
{
    public partial class TelaInicialViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string senha;

        [RelayCommand]
        private async Task CriarConta()
        {
            await Shell.Current.GoToAsync("Cadastro");
        }
    }
}
