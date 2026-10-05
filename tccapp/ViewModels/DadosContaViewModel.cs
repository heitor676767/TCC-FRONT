using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tccapp.Services.Usuarios;
// using tccapp.Services;  // namespace do UsuarioService

namespace tccapp.ViewModels
{
    public partial class DadosContaViewModel : ObservableObject
    {
        private readonly UsuarioService _usuarioService = new();

        // ---------- Só exibição (não editáveis por esse endpoint) ----------
        [ObservableProperty] private string cpf = string.Empty;
        [ObservableProperty] private string email = string.Empty;

        // ---------- Editáveis: o que o usuário digita ----------
        [ObservableProperty] private string nome = string.Empty;
        [ObservableProperty] private string telefone = string.Empty;
        [ObservableProperty] private string cep = string.Empty;
        [ObservableProperty] private string generoSelecionado;

        // ---------- Editáveis: valor atual (placeholder) ----------
        [ObservableProperty] private string nomeAntigo = string.Empty;
        [ObservableProperty] private string telefoneAntigo = string.Empty;
        [ObservableProperty] private string cepAntigo = string.Empty;
        [ObservableProperty] private string generoAntigo = string.Empty;

        // ---------- Expansão ----------
        [ObservableProperty] private bool nomeExpandido;
        [ObservableProperty] private bool emailExpandido;
        [ObservableProperty] private bool cpfExpandido;
        [ObservableProperty] private bool telefoneExpandido;
        [ObservableProperty] private bool cepExpandido;
        [ObservableProperty] private bool generoExpandido;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string mensagem = string.Empty;

        [ObservableProperty]
        private List<string> generos = new() { "Masculino", "Feminino", "Prefiro não responder" };

        public DadosContaViewModel()
        {
            CarregarDoCache();
        }

        // ---------- Toggles ----------
        [RelayCommand] private void ToggleNome() => NomeExpandido = !NomeExpandido;
        [RelayCommand] private void ToggleEmail() => EmailExpandido = !EmailExpandido;
        [RelayCommand] private void ToggleCpf() => CpfExpandido = !CpfExpandido;
        [RelayCommand] private void ToggleTelefone() => TelefoneExpandido = !TelefoneExpandido;
        [RelayCommand] private void ToggleCep() => CepExpandido = !CepExpandido;
        [RelayCommand] private void ToggleGenero() => GeneroExpandido = !GeneroExpandido;


        // Preenche com o que já está salvo no dispositivo (vindo do login),
        // já que ainda não existe um "GET meus dados" na API.
        private void CarregarDoCache()
        {
            Cpf = Preferences.Get("UsuarioCpf", string.Empty);
            Email = Preferences.Get("UsuarioEmail", string.Empty);

            NomeAntigo = Preferences.Get("UsuarioNome", string.Empty);
            TelefoneAntigo = Preferences.Get("UsuarioTelefone", string.Empty);
            CepAntigo = Preferences.Get("UsuarioCep", string.Empty);
            GeneroAntigo = Preferences.Get("UsuarioGenero", string.Empty);
        }

        // ---------- Atualizar ----------
        [RelayCommand]
        private async Task Atualizar()
        {
            if (IsBusy) return;

            // Campo em branco = mantém o valor atual
            var novoNome = string.IsNullOrWhiteSpace(Nome) ? NomeAntigo : Nome.Trim();
            var novoTelefone = string.IsNullOrWhiteSpace(Telefone) ? TelefoneAntigo : Telefone.Trim();
            var novoCep = string.IsNullOrWhiteSpace(Cep) ? CepAntigo : Cep.Trim();
            var novoGenero = string.IsNullOrWhiteSpace(GeneroSelecionado) ? GeneroAntigo : GeneroSelecionado;

            if (string.IsNullOrWhiteSpace(novoNome) ||
                string.IsNullOrWhiteSpace(novoTelefone) ||
                string.IsNullOrWhiteSpace(novoCep))
            {
                Mensagem = "Nome, telefone e CEP são obrigatórios.";
                return;
            }

            try
            {
                IsBusy = true;
                Mensagem = string.Empty;

                string token = Preferences.Get("UsuarioToken", string.Empty);

                await _usuarioService.AtualizarUsuarioAsync(
                    novoNome, novoTelefone, novoCep, novoGenero, token);

                // Atualiza o cache local com o que acabou de ser salvo
                Preferences.Set("UsuarioNome", novoNome);
                Preferences.Set("UsuarioTelefone", novoTelefone);
                Preferences.Set("UsuarioCep", novoCep);
                Preferences.Set("UsuarioGenero", novoGenero ?? string.Empty);

                // Atualiza os placeholders e limpa o que foi digitado
                NomeAntigo = novoNome;
                TelefoneAntigo = novoTelefone;
                CepAntigo = novoCep;
                GeneroAntigo = novoGenero ?? string.Empty;

                Nome = Telefone = Cep = string.Empty;
                GeneroSelecionado = null;

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
    }
}