using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tccapp.Models;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class TelaInicialViewModel : ObservableObject
    {
        private readonly UsuarioService _usuarioService = new();

        [ObservableProperty]
        private string cpf = string.Empty;

        [ObservableProperty]
        private string senha = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task CriarConta()
        {
            await Shell.Current.GoToAsync("Cadastro");
        }

        [RelayCommand]
        private async Task Autenticar()
        {
            if (string.IsNullOrWhiteSpace(Cpf) || string.IsNullOrWhiteSpace(Senha))
            {
                ErrorMessage = "Os campos não podem estar vazios";
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }

            ErrorMessage = string.Empty;
            try
            {
                Usuario u = new Usuario
                {
                    Cpf = new string(Cpf.Where(char.IsDigit).ToArray()),
                    PasswordString = Senha
                };

                Usuario autenticado = await _usuarioService.PostAutenticarUsuarioAsync(u);

                if (string.IsNullOrEmpty(autenticado.Token))
                {
                    ErrorMessage = "Dados incorretos";
                    await Task.Delay(3000);
                    ErrorMessage = string.Empty;
                    return;
                }

                Preferences.Set("UsuarioId", autenticado.Id);
                Preferences.Set("UsuarioNome", autenticado.Nome);
                Preferences.Set("UsuarioTipo", autenticado.TipoUsuario);
                Preferences.Set("UsuarioToken", autenticado.Token);

                await Shell.Current.GoToAsync("Home");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                await Task.Delay(3000);
                ErrorMessage = string.Empty;
                return;
            }
        }
    }
}