using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class VerificarEmailViewModel : ObservableObject
    {
        private readonly UsuarioService _usuarioService = new();

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        private async Task MostrarErro(string mensagem)
        {
            ErrorMessage = mensagem;
            await Task.Delay(3000);
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task Verificar()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                await MostrarErro("Preencha o e-mail");
                return;
            }

            try
            {
                await _usuarioService.EsqueciSenhaAsync(Email);
            }
            catch
            {

            }

            await Shell.Current.GoToAsync($"RecuperarSenha?email={Uri.EscapeDataString(Email)}");
        }
    }
}