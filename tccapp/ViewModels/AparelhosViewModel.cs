using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using tccapp.Models.Dto;
using tccapp.Services.Sessoes;

namespace tccapp.ViewModels
{
    public partial class AparelhosViewModel:ObservableObject
    {
        private readonly SessoesService _sessaoService;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private ObservableCollection<SessaoDto> sessoes = new();

        public AparelhosViewModel()
        {
            string token = Preferences.Get("UsuarioToken", string.Empty);
            _sessaoService = new SessoesService(token);
        }

        [RelayCommand]
        public async Task CarregarSessoesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Sessoes = await _sessaoService.ListarSessoesAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao carregar dispositivos: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SairDispositivoAsync(SessaoDto sessao)
        {
            if (sessao is null) return;

            string mensagem = sessao.Atual
                ? "Este é o seu aparelho atual. Ao sair, você voltará para a tela de login. Continuar?"
                : $"Deseja encerrar a sessão em {sessao.NomeDispositivo}?";

            bool confirmar = await Shell.Current.DisplayAlert("Desconectar", mensagem, "Sair", "Cancelar");
            if (!confirmar) return;

            try
            {
                await _sessaoService.RevogarSessaoAsync(sessao.Id);

                if (sessao.Atual)
                {
                    await DeslogarLocalmenteAsync();
                }
                else
                {
                    Sessoes.Remove(sessao);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível desconectar: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SairTodosAsync()
        {
            bool confirmar = await Shell.Current.DisplayAlert(
                "Atenção",
                "Você será desconectado de todos os aparelhos, incluindo este. Continuar?",
                "Sim, sair de tudo", "Cancelar");

            if (!confirmar) return;

            try
            {
                await _sessaoService.RevogarTodasAsync();
                await DeslogarLocalmenteAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível encerrar as sessões: {ex.Message}", "OK");
            }
        }

        private async Task DeslogarLocalmenteAsync()
        {
            Sessoes.Clear();
            Preferences.Clear();
            await Shell.Current.GoToAsync("///Inicio");
        }

        [RelayCommand]
        private async Task VoltarAsync()
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}
