using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using tccapp.Models;
using tccapp.Services.Passeios;
using tccapp.Services.Petwalkers;

namespace tccapp.ViewModels
{
    public partial class HomeViewPetwalkerModel : ObservableObject
    {
        [ObservableProperty] private bool disponivel;
        [ObservableProperty] private bool carregando;

        // Dados da notificação / solicitação recebida
        [ObservableProperty] private bool temSolicitacaoPendente;
        [ObservableProperty] private PasseioDto? solicitacaoAtual;
        [ObservableProperty] private string textoLocalizacao = "Localização via GPS";

        private CancellationTokenSource? _envioCts;
        private int _ultimoIdNotificado = 0;

        private void IniciarMonitoramento(PetwalkerService walkerService, string token)
        {
            _envioCts?.Cancel();
            _envioCts = new CancellationTokenSource();
            var ct = _envioCts.Token;
            var passeioService = new PasseioService(token);

            _ = Task.Run(async () =>
            {
                int contadorCiclos = 0;
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(6));
                try
                {
                    while (await timer.WaitForNextTickAsync(ct))
                    {
                        contadorCiclos++;

                        // 1. Verifica solicitações de passeio a cada 6 segundos
                        try
                        {
                            var meusPasseios = await passeioService.GetMeusAsync();
                            var pendente = meusPasseios?.FirstOrDefault(p => p.StatusPass == "Solicitado");

                            if (pendente != null)
                            {
                                string localFormatado = await FormatarLocalizacaoAsync(pendente);

                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    SolicitacaoAtual = pendente;
                                    TextoLocalizacao = localFormatado;
                                    TemSolicitacaoPendente = true;

                                    // Dispara alerta sonoro/visual na primeira vez que esse pedido chega
                                    if (_ultimoIdNotificado != pendente.IdPasseio)
                                    {
                                        _ultimoIdNotificado = pendente.IdPasseio;
                                        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); } catch { }
                                        await Shell.Current.DisplayAlertAsync(
                                            "Novo Pedido de Passeio!",
                                            $"Dono: {pendente.DonoNome}\nPet: {pendente.PetNome}\nDuração: {pendente.Duracao} min\nLocal: {localFormatado}",
                                            "Ver detalhes");
                                    }
                                });
                            }
                            else
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    TemSolicitacaoPendente = false;
                                    SolicitacaoAtual = null;
                                });
                            }
                        }
                        catch { /* tenta no próximo ciclo */ }

                        // 2. Atualiza GPS do petwalker a cada ~30 segundos (5 ciclos de 6s)
                        if (contadorCiclos % 5 == 0)
                        {
                            try
                            {
                                var loc = await MainThread.InvokeOnMainThreadAsync(ObterLocalizacaoAsync);
                                if (loc != null)
                                    await walkerService.AtualizarLocalizacaoAsync((decimal)loc.Latitude, (decimal)loc.Longitude);
                            }
                            catch { }
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        private static async Task<string> FormatarLocalizacaoAsync(PasseioDto passeio)
        {
            if (passeio.Localizacao == null)
                return "Localização não informada";

            if (!string.IsNullOrWhiteSpace(passeio.Localizacao.Numero))
                return $"{passeio.Localizacao.Numero} ({passeio.Localizacao.Cep})".TrimEnd(' ', '(', ')');

            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(
                    (double)passeio.Localizacao.Latitude,
                    (double)passeio.Localizacao.Longitude);
                var p = placemarks?.FirstOrDefault();
                if (p != null && !string.IsNullOrWhiteSpace(p.Thoroughfare))
                    return $"{p.Thoroughfare}, {p.SubThoroughfare} - {p.SubLocality}".TrimEnd(',', ' ', '-');
            }
            catch { }

            return $"Lat: {passeio.Localizacao.Latitude:F4}, Lng: {passeio.Localizacao.Longitude:F4}";
        }

        private void PararMonitoramento()
        {
            _envioCts?.Cancel();
            _envioCts = null;
            TemSolicitacaoPendente = false;
            SolicitacaoAtual = null;
        }

        [RelayCommand]
        private async Task AceitarSolicitacao()
        {
            if (SolicitacaoAtual == null) return;

            try
            {
                Carregando = true;
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                int idAceito = SolicitacaoAtual.IdPasseio;
                await passeioService.AceitarAsync(idAceito);

                PararMonitoramento();
                await Shell.Current.GoToAsync($"PasseioAndamento?idPasseio={idAceito}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }
        public async Task VerificarPasseioAtivoAsync(bool voltarParaHomeSeNaoHouver = false)
        {
            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                if (string.IsNullOrWhiteSpace(token))
                    return;

                var passeioService = new PasseioService(token);
                var meusPasseios = await passeioService.GetMeusAsync();

                var passeioAtivo = meusPasseios?.FirstOrDefault(p =>
                    p.StatusPass == "Aceito" || p.StatusPass == "EmAndamento");

                if (passeioAtivo != null)
                {
                    PararMonitoramento();
                    if (Shell.Current.CurrentPage is not tccapp.Telas.Passeios.PasseioAndamentoView)
                        await Shell.Current.GoToAsync($"PasseioAndamento?idPasseio={passeioAtivo.IdPasseio}");

                    return;
                }

                // Sem passeio ativo: se acionado pelo botão central fora da Home, volta para a Home do Petwalker
                if (voltarParaHomeSeNaoHouver && Shell.Current.CurrentPage is not tccapp.Telas.Home.HomeViewPetWalker)
                {
                    await Shell.Current.GoToAsync("//HomeViewPetWalker");
                }
            }
            catch { }
        }

        [RelayCommand]
        public async Task IrParaPasseioAtivo()
        {
            await VerificarPasseioAtivoAsync(voltarParaHomeSeNaoHouver: true);
        }
        [RelayCommand]
        private async Task RecusarSolicitacao()
        {
            if (SolicitacaoAtual == null) return;

            try
            {
                Carregando = true;
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                await passeioService.RecusarAsync(SolicitacaoAtual.IdPasseio);

                TemSolicitacaoPendente = false;
                SolicitacaoAtual = null;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }

        public async Task AlterarDisponibilidadeAsync(bool novoStatus)
        {
            try
            {
                Carregando = true;

                string token = Preferences.Get("UsuarioToken", string.Empty);
                if (string.IsNullOrWhiteSpace(token))
                {
                    await Shell.Current.DisplayAlertAsync("Erro", "Usuário não autenticado.", "OK");
                    return;
                }

                var service = new PetwalkerService(token);

                if (novoStatus)
                {
                    var localizacao = await ObterLocalizacaoAsync();
                    if (localizacao == null)
                    {
                        await Shell.Current.DisplayAlertAsync("Localização",
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
                if (resultado) IniciarMonitoramento(service, token);
                else PararMonitoramento();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
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

            return await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
        }
    }
}