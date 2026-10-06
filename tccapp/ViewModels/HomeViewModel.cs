using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using tccapp.Models;
using tccapp.Services.Passeios;
using tccapp.Services.Pets;
using tccapp.Services.Petwalkers;

namespace tccapp.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        public Mapsui.Map Map { get; }

        private readonly MemoryLayer _localizacaoLayer;

        // Localização mais recente conhecida do usuário — usada tanto pra buscar
        // petwalkers por perto quanto pra enviar junto da solicitação de passeio.
        private double? _minhaLatitude;
        private double? _minhaLongitude;

        [ObservableProperty]
        private ObservableCollection<PetwalkerPerfil> petwalkers = new();

        [ObservableProperty]
        private bool carregandoPetwalkers;

        public HomeViewModel()
        {
            Map = new Mapsui.Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer("TCCApp"));

            _localizacaoLayer = new MemoryLayer
            {
                Name = "MinhaLocalizacao",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 43, 108, 176)),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
                }
            };
            Map.Layers.Add(_localizacaoLayer);

            // posição inicial enquanto o GPS não responde
            var (x, y) = SphericalMercator.FromLonLat(-46.5961203, -23.5189015);
            Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 10);
        }

        public async Task IniciarRastreamentoAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                //if (status != PermissionStatus.Granted)
                //{
                //    await Shell.Current.DisplayAlertAsync("Localização", $"Permissão: {status}", "Ok");
                //    return;
                //}

                Geolocation.Default.LocationChanged -= OnLocationChanged;
                Geolocation.Default.LocationChanged += OnLocationChanged;

                if (!Geolocation.Default.IsListeningForeground)
                {
                    var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));
                    await Geolocation.Default.StartListeningForegroundAsync(request);
                }

                var localizacao = await Geolocation.Default.GetLastKnownLocationAsync()
                    ?? await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

                if (localizacao == null)
                {
                    await Shell.Current.DisplayAlertAsync("Localização", "Não foi possível obter a posição.", "Ok");
                    return;
                }

                AtualizarPosicaoNoMapa(localizacao, centralizar: true);

                // Só busca a lista de petwalkers na primeira vez que a posição é obtida,
                // pra não ficar chamando a API a cada atualização de GPS.
                await CarregarPetwalkersAsync(localizacao.Latitude, localizacao.Longitude);

                // Se quem logou é (ou também é) petwalker, já manda a posição atual pra API,
                // igual ao usuário comum — sem precisar de nenhuma tela/botão separado.
                await AtualizarMinhaLocalizacaoDePetwalkerAsync(localizacao.Latitude, localizacao.Longitude);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro de localização", ex.Message, "Ok");
            }
        }

        private void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => AtualizarPosicaoNoMapa(e.Location, centralizar: true));
        }

        private void AtualizarPosicaoNoMapa(Location location, bool centralizar)
        {
            _minhaLatitude = location.Latitude;
            _minhaLongitude = location.Longitude;

            var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var ponto = new MPoint(x, y);

            _localizacaoLayer.Features = new List<IFeature> { new PointFeature(ponto) };
            _localizacaoLayer.DataHasChanged();

            if (centralizar)
                Map.Navigator.CenterOnAndZoomTo(ponto, 3);
        }

        public void PararRastreamento()
        {
            if (Geolocation.Default.IsListeningForeground)
                Geolocation.Default.StopListeningForeground();

            Geolocation.Default.LocationChanged -= OnLocationChanged;
        }

        private async Task AtualizarMinhaLocalizacaoDePetwalkerAsync(double lat, double lng)
        {
            string tipoUsuario = Preferences.Get("UsuarioTipo", string.Empty);
            bool ehPetwalker = tipoUsuario == "Petwalker";

            if (!ehPetwalker)
                return;

            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var petwalkerService = new PetwalkerService(token);
                await petwalkerService.AtualizarLocalizacaoAsync((decimal)lat, (decimal)lng);
            }
            catch
            {
                // Silencioso de propósito: isso não pode travar a tela de quem é só Dono,
                // nem incomodar o petwalker toda vez que abre o app por causa de rede instável.
            }
        }

        private async Task CarregarPetwalkersAsync(double lat, double lng)
        {
            try
            {
                CarregandoPetwalkers = true;

                string token = Preferences.Get("UsuarioToken", string.Empty);
                var petwalkerService = new PetwalkerService(token);

                var resultado = await petwalkerService.GetDisponiveisAsync(lat, lng, raioKm: 10);
                Petwalkers = resultado ?? new ObservableCollection<PetwalkerPerfil>();
            }
            catch (Exception ex)
            {
                // Não interrompe o uso do mapa por causa disso, só avisa.
                await Shell.Current.DisplayAlertAsync("Petwalkers", $"Não foi possível carregar a lista: {ex.Message}", "Ok");
            }
            finally
            {
                CarregandoPetwalkers = false;
            }
        }

        [RelayCommand]
        private async Task Contratar(PetwalkerPerfil petwalker)
        {
            if (petwalker == null)
                return;

            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                string cpfDono = Preferences.Get("UsuarioCpf", string.Empty);

                var petService = new PetService(token);
                var meusPets = await petService.GetMeusPetsAsync(cpfDono);

                if (meusPets == null || !meusPets.Any())
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Cadastre um pet",
                        "Você precisa cadastrar pelo menos um pet antes de contratar um passeio.",
                        "Ok");
                    return;
                }

                // MVP: usa o primeiro pet cadastrado. Quando tiver tempo, trocar por
                // uma tela/seletor pra escolher qual pet vai passear.
                var pet = meusPets.First();

                if (_minhaLatitude == null || _minhaLongitude == null)
                {
                    await Shell.Current.DisplayAlertAsync("Localização", "Aguarde a localização carregar e tente de novo.", "Ok");
                    return;
                }

                var passeioService = new PasseioService(token);
                await passeioService.SolicitarAsync(
                    rga: pet.Rga,
                    cpfPetwalker: petwalker.Cpf,
                    duracao: 30,
                    latitude: (decimal)_minhaLatitude.Value,
                    longitude: (decimal)_minhaLongitude.Value,
                    cep: string.Empty,
                    numero: string.Empty);

                await Shell.Current.DisplayAlertAsync(
                    "Solicitado!",
                    $"Passeio com {petwalker.Nome} solicitado para {pet.Nome}. Aguardando o petwalker aceitar.",
                    "Ok");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro ao contratar", ex.Message, "Ok");
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