using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class InfoContaViewModel : ObservableObject
    {
        private readonly UsuarioService _usuarioService = new();

        // Só exibição, não editáveis por esse endpoint
        [ObservableProperty]
        private string cpf = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        // Editáveis
        [ObservableProperty]
        private string nome = string.Empty;

        [ObservableProperty]
        private string telefone = string.Empty;

        [ObservableProperty]
        private string cep = string.Empty;

        [ObservableProperty]
        private string genero = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string mensagem = string.Empty;

        public InfoContaViewModel()
        {
            CarregarDoCache();
        }

        // Preenche com o que já está salvo no dispositivo (vindo do login),
        // já que ainda não existe um "GET meus dados" na API.
        private void CarregarDoCache()
        {
            Cpf = Preferences.Get("UsuarioCpf", string.Empty);
            Email = Preferences.Get("UsuarioEmail", string.Empty);
            Nome = Preferences.Get("UsuarioNome", string.Empty);
            Telefone = Preferences.Get("UsuarioTelefone", string.Empty);
            Cep = Preferences.Get("UsuarioCep", string.Empty);
            Genero = Preferences.Get("UsuarioGenero", string.Empty);
        }

        [RelayCommand]
        private async Task Atualizar()
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Telefone) || string.IsNullOrWhiteSpace(Cep))
            {
                Mensagem = "Nome, telefone e CEP são obrigatórios.";
                return;
            }

            try
            {
                IsBusy = true;
                Mensagem = string.Empty;

                string token = Preferences.Get("UsuarioToken", string.Empty);

                await _usuarioService.AtualizarUsuarioAsync(Nome, Telefone, Cep, Genero, token);

                // Atualiza o cache local com o que acabou de ser salvo
                Preferences.Set("UsuarioNome", Nome);
                Preferences.Set("UsuarioTelefone", Telefone);
                Preferences.Set("UsuarioCep", Cep);
                Preferences.Set("UsuarioGenero", Genero);

                Mensagem = "Dados atualizados!";
            }
            catch (System.Exception ex)
            {
                Mensagem = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CadastroPet()
        {
            await Shell.Current.GoToAsync("CadastroPet");
        }

        [RelayCommand]
        private async Task Home()
        {
            await Shell.Current.GoToAsync("Home");
        }

        [RelayCommand]
        private async Task InfoConta()
        {
            await Shell.Current.GoToAsync("InfoConta");
        }


        [RelayCommand]
        private async Task Dados()
        {
            await Shell.Current.GoToAsync("Dados");
        }

        [RelayCommand]
        private async Task Sair()
        {
            await Shell.Current.GoToAsync("Inicio");
        }
    }
}
