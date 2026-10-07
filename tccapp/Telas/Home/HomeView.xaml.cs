using Mapsui;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors; // Adicionado para o Geocoding
using tccapp.ViewModels;

namespace tccapp.Telas.Home;

public partial class HomeView : ContentPage
{
    HomeViewModel viewModel;

    public HomeView()
    {
        InitializeComponent();

        viewModel = new HomeViewModel();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.IniciarRastreamentoAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.PararRastreamento();
    }

    private async void Mapa_MapTapped(object? sender, MapEventArgs e)
    {
        try
        {
            var mapInfo = e.GetMapInfo(
                e.Map.Layers.Where(layer => layer.Name == "Petshops"));

            var feature = mapInfo?.Feature;

            if (feature == null)
                return;

            var nome = feature["Nome"]?.ToString() ?? "Pet shop";
            var endereco = feature["Endereco"]?.ToString() ?? "Endereço não informado";

            double latitude = Convert.ToDouble(feature["Latitude"]);
            double longitude = Convert.ToDouble(feature["Longitude"]);

            // SE O ENDEREÇO ESTIVER VAZIO, BUSCAMOS PELO GPS AGORA!
            if (endereco == "Endereço não informado")
            {
                try
                {
                    var placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);
                    var placemark = placemarks?.FirstOrDefault();

                    if (placemark != null)
                    {
                        var geoPartes = new List<string>();

                        if (!string.IsNullOrWhiteSpace(placemark.Thoroughfare))
                        {
                            string ruaNum = placemark.Thoroughfare;
                            if (!string.IsNullOrWhiteSpace(placemark.SubThoroughfare))
                                ruaNum += $", {placemark.SubThoroughfare}";

                            geoPartes.Add(ruaNum);
                        }

                        if (!string.IsNullOrWhiteSpace(placemark.SubLocality))
                            geoPartes.Add(placemark.SubLocality);

                        if (!string.IsNullOrWhiteSpace(placemark.Locality))
                            geoPartes.Add(placemark.Locality);

                        if (geoPartes.Count > 0)
                        {
                            endereco = string.Join(" - ", geoPartes);

                            // Salvamos na feature para não precisar buscar de novo se você clicar duas vezes no mesmo pin!
                            feature["Endereco"] = endereco;
                        }
                    }
                }
                catch
                {
                    // Se falhar (ex: sem internet na hora do clique), ele apenas mantém o "Endereço não informado"
                }
            }

            string acao = await DisplayActionSheetAsync(
                nome,
                "Fechar",
                null,
                endereco);

            if (acao == endereco && endereco != "Endereço não informado")
            {
                string lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lng = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

                string url = $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}";

                await Launcher.Default.OpenAsync(url);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro",
                $"Não foi possível abrir as informações do pet shop.\n\n{ex.Message}",
                "OK");
        }
    }
}