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
using System.Threading.Tasks;

namespace tccapp.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        public Mapsui.Map Map { get; }

        private readonly MemoryLayer _localizacaoLayer;

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