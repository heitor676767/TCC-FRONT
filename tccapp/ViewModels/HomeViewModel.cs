using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Widgets;
using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.ViewModels

{
    public partial class HomeViewModel : ObservableObject
    {
        public Mapsui.Map Map { get; }
        
        public HomeViewModel()
        {
            Map = new Mapsui.Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer("TCCApp"));

            var (x, y) = SphericalMercator.FromLonLat(-46.5961203, -23.5189015);
            var centro = new MPoint(x, y);

            Map.Navigator.CenterOnAndZoomTo(centro, 15);

        }
        [RelayCommand]
        private async Task CadastroPet()
        {
            await Shell.Current.GoToAsync("CadastroPet");
        }
    }
}
