using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tccapp.Models;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class UserCadastroViewModel : ObservableObject
    {
        private readonly UsuarioService _usuarioService = new();

        [ObservableProperty]
        private List<string> generos = new() { "Masculino", "Feminino", "Prefiro não responder" };

        [ObservableProperty]
        private List<string> tipoUsuario = new() { "Dono", "Petwalker", "Ambos" };

        [ObservableProperty]
        private string generoSelecionado;

        [ObservableProperty]
        private string tipoSelecionado;

        [ObservableProperty]
        private string cpf = string.Empty;

        [ObservableProperty]
        private string senha = string.Empty;

        [ObservableProperty]
        private string nome = string.Empty;

        [ObservableProperty]
        private string cep = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string telefone = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public string TipoExibido => string.IsNullOrEmpty(TipoSelecionado) ? "Selecione o tipo de conta desejado" : TipoSelecionado;
        public Color TipoTextColor => string.IsNullOrEmpty(TipoSelecionado) ? Color.Parse("#999999") : Colors.Black;

        public string GeneroExibido => string.IsNullOrEmpty(GeneroSelecionado) ? "Selecione o Gênero" : GeneroSelecionado;
        public Color GeneroTextColor => string.IsNullOrEmpty(GeneroSelecionado) ? Color.Parse("#999999") : Colors.Black;

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        partial void OnGeneroSelecionadoChanged(string value)
        {
            OnPropertyChanged(nameof(GeneroExibido));
            OnPropertyChanged(nameof(GeneroTextColor));
        }

        partial void OnTipoSelecionadoChanged(string value)
        {
            OnPropertyChanged(nameof(TipoExibido));
            OnPropertyChanged(nameof(TipoTextColor));
        }

        private async Task MostrarErro(string mensagem)
        {
            ErrorMessage = mensagem;
            await Task.Delay(3000);
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task Registrar()
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Telefone) || string.IsNullOrWhiteSpace(Cep) ||
                string.IsNullOrWhiteSpace(Cpf) || string.IsNullOrWhiteSpace(Senha) ||
                string.IsNullOrWhiteSpace(GeneroSelecionado) || string.IsNullOrWhiteSpace(TipoSelecionado))
            {
                await MostrarErro("Preencha todos os campos");
                return;
            }

            string cpfLimpo = new string(Cpf.Where(char.IsDigit).ToArray());

            try
            {
                bool emailExiste = await _usuarioService.VerificarEmailAsync(Email);
                if (emailExiste)
                {
                    await MostrarErro("E-mail já cadastrado");
                    return;
                }

                bool cpfExiste = await _usuarioService.VerificarCpfAsync(cpfLimpo);
                if (cpfExiste)
                {
                    await MostrarErro("CPF já cadastrado");
                    return;
                }

                bool telefoneExiste = await _usuarioService.VerificarTelefoneAsync(Telefone);
                if (telefoneExiste)
                {
                    await MostrarErro("Telefone já cadastrado");
                    return;
                }

                Usuario u = new Usuario
                {
                    Nome = Nome,
                    Email = Email,
                    Telefone = Telefone,
                    Cep = Cep,
                    Cpf = cpfLimpo,
                    PasswordString = Senha,
                    Genero = GeneroSelecionado,
                    TipoUsuario = TipoSelecionado
                };

                Usuario registrado = await _usuarioService.PostRegistrarUsuarioAsync(u);

                if (registrado.Id != 0)
                {
                    await Shell.Current.DisplayAlertAsync("Sucesso", "Conta criada com sucesso!", "Ok");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await MostrarErro("Não foi possível criar a conta");
                }
            }
            catch (Exception ex)
            {

                await MostrarErro(ex.Message);
            }
        }
    }
}