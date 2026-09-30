using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tccapp.Models;
using tccapp.Models.Enums;
using tccapp.Services.Pets;

namespace tccapp.ViewModels
{
    public partial class PetCadastroViewModel : ObservableObject
    {
        private readonly PetService _petService;

        [ObservableProperty]
        private string porteSelecionado;

        [ObservableProperty]
        private string generoSelecionado;

        [ObservableProperty]
        private List<string> generos = new() { "Masculino", "Feminino" };

        [ObservableProperty]
        private List<string> portes = new() { "Grande", "Médio", "Pequeno" };

        [ObservableProperty] private string rga = string.Empty;
        [ObservableProperty] private string nome = string.Empty;
        [ObservableProperty] private string especie = string.Empty;
        [ObservableProperty] private string raca = string.Empty;
        [ObservableProperty] private string descricao = string.Empty;
        [ObservableProperty] private string peso = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        public PetCadastroViewModel()
        {
            string token = Preferences.Get("UsuarioToken", string.Empty);
            _petService = new PetService(token);
        }

        private async Task MostrarErro(string mensagem)
        {
            ErrorMessage = mensagem;
            await Task.Delay(3000);
            ErrorMessage = string.Empty;
        }

        private tccapp.Models.Enums.Porte? ConverterPorte(string valor) => valor switch
        {
            "Pequeno" => tccapp.Models.Enums.Porte.Pequeno,
            "Médio" => tccapp.Models.Enums.Porte.Medio,
            "Grande" => tccapp.Models.Enums.Porte.Grande,
            _ => null
        };

        private Sexo? ConverterSexo(string valor) => valor switch
        {
            "Masculino" => Sexo.Macho,
            "Feminino" => Sexo.Femea,
            _ => null
        };

        [RelayCommand]
        private async Task Registrar()
        {
            if (string.IsNullOrWhiteSpace(Rga) || string.IsNullOrWhiteSpace(Nome) ||
                string.IsNullOrWhiteSpace(Especie) || string.IsNullOrWhiteSpace(Raca) ||
                string.IsNullOrWhiteSpace(Peso) || string.IsNullOrEmpty(PorteSelecionado) ||
                string.IsNullOrEmpty(GeneroSelecionado))
            {
                await MostrarErro("Preencha todos os campos");
                return;
            }

            if (!int.TryParse(Peso, out int pesoInt) || pesoInt <= 0)
            {
                await MostrarErro("Peso inválido");
                return;
            }

            tccapp.Models.Enums.Porte? porteEnum = ConverterPorte(PorteSelecionado);
            Sexo? sexoEnum = ConverterSexo(GeneroSelecionado);

            if (porteEnum == null || sexoEnum == null)
            {
                await MostrarErro("Selecione porte e gênero válidos");
                return;
            }

            string cpfDono = Preferences.Get("UsuarioCpf", string.Empty);
            if (string.IsNullOrEmpty(cpfDono))
            {
                await MostrarErro("Faça login novamente");
                return;
            }

            try
            {
                var p = new Pet
                {
                    Rga = Rga.Trim(),
                    Nome = Nome.Trim(),
                    Especie = Especie.Trim(),
                    Raca = Raca.Trim(),
                    Descricao = Descricao?.Trim() ?? string.Empty,
                    Peso = pesoInt,
                    Porte = porteEnum.Value,
                    Sexo = sexoEnum.Value,
                    CpfDono = cpfDono
                };

                int id = await _petService.PostRegistrarPetAsync(p);
                if (id != 0)
                    await Shell.Current.DisplayAlertAsync("Sucesso", "Pet cadastrado com sucesso!", "Ok");
                else
                    await MostrarErro("Não foi possível cadastrar o pet");
            }
            catch (Exception ex)
            {
                await MostrarErro(ex.Message);
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
    }
}