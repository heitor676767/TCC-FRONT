using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class RecuperarSenhaViewModel:ObservableObject, IQueryAttributable
    {
        private readonly UsuarioService _usuarioService = new();
        [ObservableProperty]
        private string code = string.Empty;
        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string senha = string.Empty;

        [ObservableProperty]
        private string senha2 = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("email", out var email))
            {
                Email = email?.ToString() ?? string.Empty;
            }
        }

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task Verify()
        {
            if (string.IsNullOrWhiteSpace(Code) ||
                string.IsNullOrWhiteSpace(Senha) ||
                string.IsNullOrWhiteSpace(Senha2))
            {
                ErrorMessage = "Os campos não podem estar vazios.";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }

            if (Code.Length != 6)
            {
                ErrorMessage = "O código deve ter 6 dígitos.";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }

            if (Senha != Senha2)
            {
                ErrorMessage = "Por favor, reconfirme a senha.";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }

            try
            {
                await _usuarioService.RedefinirSenhaAsync(
                    Email,
                    Code,
                    Senha
                );

                ErrorMessage = string.Empty;

                await Shell.Current.GoToAsync("Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{ex}";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
            }
        }
    }
}

