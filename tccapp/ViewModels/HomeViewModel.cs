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
using tccapp.Services.Petshops;
using tccapp.Services.Petwalkers;
using tccapp.Services.Usuarios;

namespace tccapp.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly PetShopService _petShopService = new();
        public Mapsui.Map Map { get; }

        private readonly MemoryLayer _localizacaoLayer;
        private readonly MemoryLayer _petwalkersLayer;
        private readonly MemoryLayer _petshopsLayer;

        // Localização mais recente conhecida do usuário — usada tanto pra buscar
        // petwalkers por perto quanto pra enviar junto da solicitação de passeio.
        private double? _minhaLatitude;
        private double? _minhaLongitude;

        [ObservableProperty]
        private ObservableCollection<PetwalkerPerfil> petwalkers = new();

        [ObservableProperty]
        private bool carregandoPetwalkers;

        [ObservableProperty]
        private bool menuPetsAberto;

        [ObservableProperty]
        private ObservableCollection<Pet> meusPets = new();

        public HomeViewModel()
        {
            

            Map = new Mapsui.Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer("TCCApp"));

            Map.Widgets.Clear();
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

            _petwalkersLayer = new MemoryLayer
            {
                Name = "Petwalkers",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Mapsui.Styles.Brush(
            Mapsui.Styles.Color.FromArgb(255, 220, 38, 127)),
                    Outline = new Mapsui.Styles.Pen(
            Mapsui.Styles.Color.White, 2)
                }
            };

            Map.Layers.Add(_petwalkersLayer);

            _petshopsLayer = new MemoryLayer
            {
                Name = "Petshops",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 255, 140, 0)), // laranja
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
                }
            };
            Map.Layers.Add(_petshopsLayer);

            // posição inicial enquanto o GPS não responde
            var (x, y) = SphericalMercator.FromLonLat(-46.5961203, -23.5189015);
            Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 10);
        }

        [RelayCommand]
        private async Task AbrirMenuPets()
        {
            MenuPetsAberto = true;
            await CarregarMeusPetsAsync();
        }

        [RelayCommand]
        private void FecharMenuPets()
        {
            MenuPetsAberto = false;
        }

        private async Task CarregarMeusPetsAsync()
        {
            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                string cpfDono = Preferences.Get("UsuarioCpf", string.Empty);

                var petService = new PetService(token);
                var pets = await petService.GetMeusPetsAsync(cpfDono);

                MeusPets.Clear();
                if (pets != null)
                {
                    foreach (var pet in pets)
                        MeusPets.Add(pet);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Pets", $"Não foi possível carregar seus pets: {ex.Message}", "Ok");
            }
        }

        private async Task CarregarPetshopsAsync(double lat, double lng)
        {
            try
            {
                var service = new PetShopService();
                var petshops = await service.BuscarProximosAsync(lat, lng, raioMetros: 5000);

                var features = new List<IFeature>();
                foreach (var p in petshops)
                {
                    var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                    var feature = new PointFeature(new MPoint(x, y));
                    feature["Nome"] = p.Nome;
                    feature["Endereco"] = p.Endereco;
                    feature["DistanciaKm"] = p.DistanciaKm;
                    feature["Latitude"] = p.Latitude;
                    feature["Longitude"] = p.Longitude;
                    feature["Endereco"] = p.Endereco;

                    features.Add(feature);
                }

                _petshopsLayer.Features = features;
                _petshopsLayer.DataHasChanged();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Erro nos Petshops",
                    ex.Message,
                    "OK");
            }
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
                await CarregarPetshopsAsync(localizacao.Latitude, localizacao.Longitude);

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
            MainThread.BeginInvokeOnMainThread(() => AtualizarPosicaoNoMapa(e.Location, centralizar: false));
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
                Petwalkers.Clear();
                if (resultado != null)
                {
                    foreach (var walker in resultado)
                    {
                        Petwalkers.Add(walker);
                    }
                }

                AtualizarPetwalkersNoMapa(resultado);
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

        private void AtualizarPetwalkersNoMapa(IEnumerable<PetwalkerPerfil> petwalkers)
        {
            var features = new List<IFeature>();

            foreach (var petwalker in petwalkers)
            {
                if (!petwalker.Latitude.HasValue || !petwalker.Longitude.HasValue)
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(
                    (double)petwalker.Longitude.Value,
                    (double)petwalker.Latitude.Value);

                var feature = new PointFeature(new MPoint(x, y));

                // Guarda o CPF no Feature para conseguirmos identificar
                // qual Petwalker foi clicado depois.
                feature["Cpf"] = petwalker.Cpf;
                feature["Nome"] = petwalker.Nome;

                features.Add(feature);
            }

            _petwalkersLayer.Features = features;
            _petwalkersLayer.DataHasChanged();
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