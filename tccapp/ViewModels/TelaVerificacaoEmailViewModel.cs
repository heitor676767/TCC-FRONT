using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace tccapp.ViewModels
{
    public partial class TelaVerificacaoEmailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string code = string.Empty;
        [ObservableProperty]
        private string errorMessage = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }
        public ObservableCollection<string> Digits { get; } = new(new string[6] { "", "", "", "", "", "" });

        partial void OnCodeChanged(string value)
        {
            for (int i = 0; i < 6; i++)
                Digits[i] = i < value.Length ? value[i].ToString() : "";
        }

        [RelayCommand]
        private async Task OutroMetodo()
        {
            await Shell.Current.GoToAsync("Telefone");
        }
        [RelayCommand]
        private async Task OutroMetodo2()
        {
            await Shell.Current.GoToAsync("Verificar");
        }




        [RelayCommand]
        private async Task Verify()
        {
            if (Code.Length != 6)
            {
                ErrorMessage = "O código deve ter 6 dígitos.";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage = string.Empty;

            // lógica de verificação
        }
    }
}
