using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using tccapp.Models.Dto;
using tccapp.Services.Carteiras;


namespace tccapp.ViewModels
{
    public partial class CartoesViewModel : ObservableObject
    {
        private readonly CartoesService _cartoesService;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool popupAberto;

        [ObservableProperty]
        private ObservableCollection<CartaoDto> cartoes = new();

        [ObservableProperty]
        private string numeroCartao = string.Empty;

        [ObservableProperty]
        private string nomeTitular = string.Empty;

        [ObservableProperty]
        private string validade = string.Empty;

        [ObservableProperty]
        private string cvv = string.Empty;

        [ObservableProperty]
        private string tipoSelecionado = "Crédito";

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public List<string> TiposCartao { get; } = new() { "Crédito", "Débito" };

        public CartoesViewModel()
        {
            string token = Preferences.Get("UsuarioToken", string.Empty);
            _cartoesService = new CartoesService(token);
        }

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
        public async Task CarregarCartoesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Cartoes = await _cartoesService.ListarCartoesAsync();
            }
            catch (Exception ex)
            {
                await MostrarErro($"Falha ao carregar cartões: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AbrirPopup()
        {
            NumeroCartao = string.Empty;
            NomeTitular = string.Empty;
            Validade = string.Empty;
            Cvv = string.Empty;
            TipoSelecionado = "Crédito";
            ErrorMessage = string.Empty;
            PopupAberto = true;
        }

        [RelayCommand]
        private void FecharPopup()
        {
            ErrorMessage = string.Empty;
            PopupAberto = false;
        }

        [RelayCommand]
        private async Task SalvarCartaoAsync()
        {
            if (IsBusy) return;

            string numeroLimpo = new string((NumeroCartao ?? string.Empty).Where(char.IsDigit).ToArray());
            string cvvLimpo = new string((Cvv ?? string.Empty).Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(numeroLimpo) ||
                string.IsNullOrWhiteSpace(NomeTitular) ||
                string.IsNullOrWhiteSpace(Validade) ||
                string.IsNullOrWhiteSpace(cvvLimpo))
            {
                await MostrarErro("Preencha todos os campos do cartão");
                return;
            }

            if (numeroLimpo.Length < 13 || numeroLimpo.Length > 19 || !ValidarLuhn(numeroLimpo))
            {
                await MostrarErro("Número de cartão inválido");
                return;
            }

            if (NomeTitular.Trim().Length < 3)
            {
                await MostrarErro("Informe o nome completo impresso no cartão");
                return;
            }

            if (!ValidarDataExpiracao(Validade))
            {
                await MostrarErro("Validade inválida ou cartão vencido (use MM/AA)");
                return;
            }

            if (cvvLimpo.Length < 3 || cvvLimpo.Length > 4)
            {
                await MostrarErro("CVV inválido (deve ter 3 ou 4 dígitos)");
                return;
            }

            try
            {
                IsBusy = true;

                var novoCartao = new
                {
                    NumeroCartao = numeroLimpo,
                    NomeTitular = NomeTitular.Trim(),
                    Validade = Validade.Trim(),
                    Cvv = cvvLimpo,
                    Tipo = TipoSelecionado
                };

                var criado = await _cartoesService.AdicionarCartaoAsync(novoCartao);
                Cartoes.Insert(0, criado);
                PopupAberto = false;
            }
            catch (Exception ex)
            {
                await MostrarErro($"Não foi possível salvar o cartão: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RemoverCartaoAsync(CartaoDto cartao)
        {
            if (cartao is null) return;

            bool confirmar = await Shell.Current.DisplayAlert(
                "Remover cartão",
                $"Deseja remover o cartão {cartao.TituloExibicao} ({cartao.NumeroMascarado})?",
                "Remover", "Cancelar");

            if (!confirmar) return;

            try
            {
                await _cartoesService.RemoverCartaoAsync(cartao.Id);
                Cartoes.Remove(cartao);
            }
            catch (Exception ex)
            {
                await MostrarErro($"Não foi possível remover: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task VoltarAsync()
        {
            await Shell.Current.Navigation.PopAsync();
        }

        private static bool ValidarLuhn(string numero)
        {
            int soma = 0;
            bool dobrar = false;

            for (int i = numero.Length - 1; i >= 0; i--)
            {
                int digito = numero[i] - '0';
                if (dobrar)
                {
                    digito *= 2;
                    if (digito > 9) digito -= 9;
                }
                soma += digito;
                dobrar = !dobrar;
            }

            return soma % 10 == 0;
        }

        private static bool ValidarDataExpiracao(string validade)
        {
            var partes = validade.Trim().Split('/');
            if (partes.Length != 2) return false;

            if (!int.TryParse(partes[0], out int mes) || mes < 1 || mes > 12)
                return false;

            if (!int.TryParse(partes[1], out int ano) || partes[1].Length != 2)
                return false;

            int anoCompleto = 2000 + ano;
            var ultimoDiaDoMes = new DateTime(anoCompleto, mes, DateTime.DaysInMonth(anoCompleto, mes));

            return ultimoDiaDoMes >= DateTime.Today;
        }
    }
}