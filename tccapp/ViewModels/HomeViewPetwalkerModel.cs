using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices.Sensors;
using tccapp.Services.Petwalkers;

namespace tccapp.ViewModels
{
    public partial class HomeViewPetwalkerModel : ObservableObject
    {
        [ObservableProperty] private bool disponivel;
        [ObservableProperty] private bool carregando;

        private CancellationTokenSource? _envioCts;

        private void IniciarEnvioPosicao(PetwalkerService service)
        {
            _envioCts?.Cancel();
            _envioCts = new CancellationTokenSource();
            var ct = _envioCts.Token;

            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
                try
                {
                    while (await timer.WaitForNextTickAsync(ct))
                    {
                        try
                        {
                            var loc = await MainThread.InvokeOnMainThreadAsync(ObterLocalizacaoAsync);
                            if (loc != null)
                                await service.AtualizarLocalizacaoAsync((decimal)loc.Latitude, (decimal)loc.Longitude);
                        }
                        catch { /* tenta de novo no próximo ciclo */ }
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        private void PararEnvioPosicao()
        {
            _envioCts?.Cancel();
            _envioCts = null;
        }

        public async Task AlterarDisponibilidadeAsync(bool novoStatus)
        {
            try
            {
                Carregando = true;

                string token = Preferences.Get("UsuarioToken", string.Empty);
                if (string.IsNullOrWhiteSpace(token))
                {
                    await Shell.Current.DisplayAlert("Erro", "Usuário não autenticado.", "OK");
                    return;
                }

                var service = new PetwalkerService(token);

                // Ao ATIVAR, grava a posição atual antes de ficar disponível.
                // Se falhar, não ativa: senão ele ficaria "disponível" sem coordenada
                // e continuaria invisível na busca.
                if (novoStatus)
                {
                    var localizacao = await ObterLocalizacaoAsync();
                    if (localizacao == null)
                    {
                        await Shell.Current.DisplayAlert("Localização",
                            "Não foi possível obter sua posição. Verifique o GPS e a permissão de localização.", "OK");
                        Disponivel = false;
                        return;
                    }

                    await service.AtualizarLocalizacaoAsync(
                        (decimal)localizacao.Latitude,
                        (decimal)localizacao.Longitude);
                }

                bool resultado = await service.AtualizarDisponibilidadeAsync(novoStatus);
                Disponivel = resultado;
                if (resultado) IniciarEnvioPosicao(service);
                else PararEnvioPosicao();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }

        private static async Task<Location?> ObterLocalizacaoAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return null;

            // Posição fresca, não a "última conhecida", que pode estar velha.
            return await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
        }
    }
}