using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace tccapp.Services.Petshops
{
    public class Petshop
    {
        public string Nome { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanciaKm { get; set; }
    }
    public class PetShopService
    {
        private static readonly HttpClient _http = CriarClient();

        private static HttpClient CriarClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            // A Overpass pede um User-Agent identificável
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TCCApp/1.0");
            return client;
        }

        public async Task<List<Petshop>> BuscarProximosAsync(double lat, double lng, int raioMetros = 30000, int maximo = 20)
        {
            string la = lat.ToString(CultureInfo.InvariantCulture);
            string lo = lng.ToString(CultureInfo.InvariantCulture);

            string query = $@"[out:json][timeout:15];
(
  node[""shop""=""pet""](around:{raioMetros},{la},{lo});
  way[""shop""=""pet""](around:{raioMetros},{la},{lo});
);
out center;";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var resposta = await _http.PostAsync("https://overpass-api.de/api/interpreter", content);
            resposta.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var lista = new List<Petshop>();

            foreach (var el in doc.RootElement.GetProperty("elements").EnumerateArray())
            {
                double pLat, pLng;

                if (el.TryGetProperty("lat", out var latEl))
                {
                    pLat = latEl.GetDouble();
                    pLng = el.GetProperty("lon").GetDouble();
                }
                else if (el.TryGetProperty("center", out var c))
                {
                    pLat = c.GetProperty("lat").GetDouble();
                    pLng = c.GetProperty("lon").GetDouble();
                }
                else continue;

                string nome = "Pet shop";
                if (el.TryGetProperty("tags", out var tags) && tags.TryGetProperty("name", out var n))
                    nome = n.GetString() ?? nome;

                lista.Add(new Petshop
                {
                    Nome = nome,
                    Latitude = pLat,
                    Longitude = pLng,
                    DistanciaKm = Haversine(lat, lng, pLat, pLng)
                });
            }

            return lista.OrderBy(p => p.DistanciaKm).Take(maximo).ToList();
        }

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    
}
}
