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
        public string Endereco { get; set; }
    }
    public class PetShopService
    {
        private static readonly HttpClient _http = CriarClient();

        private static readonly string[] Endpoints =
            [
                "https://overpass-api.de/api/interpreter",
                "https://overpass.kumi.systems/api/interpreter",
                "https://overpass.private.coffee/api/interpreter"
            ];

        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static List<Petshop>? _cache;
        private static double _cacheLat, _cacheLng;
        private static DateTime _cacheEm;




        private static HttpClient CriarClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            // A Overpass pede um User-Agent identificável
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TCCApp/1.0");
            return client;
        }

        public async Task<List<Petshop>> BuscarProximosAsync(double lat, double lng, int raioMetros = 2500, int maximo = 20)
        {
            await _lock.WaitAsync();
            try
            {
                if (_cache != null
                    && DateTime.UtcNow - _cacheEm < TimeSpan.FromMinutes(30)
                    && Haversine(lat, lng, _cacheLat, _cacheLng) < 0.5)
                    return _cache;

                Exception? ultimoErro = null;

                foreach (var url in Endpoints)
                {
                    try
                    {
                        var resultado = await BuscarAsync(url, lat, lng, raioMetros, maximo);
                        if (resultado.Count > 0)
                        {
                            _cache = resultado;
                            _cacheLat = lat;
                            _cacheLng = lng;
                            _cacheEm = DateTime.UtcNow;
                        }
                        return resultado;
                    }
                    catch (Exception ex)
                    {
                        ultimoErro = ex;
                    }
                }

                if (_cache != null)
                    return _cache;

                throw ultimoErro!;
            }
            finally
            {
                _lock.Release();
            }
        }


        private async Task<List<Petshop>> BuscarAsync(string url, double lat, double lng, int raioMetros, int maximo)
        {
            string la = lat.ToString(CultureInfo.InvariantCulture);
            string lo = lng.ToString(CultureInfo.InvariantCulture);

            string query = $@"[out:json][timeout:15];
nwr[""shop""=""pet""](around:{raioMetros},{la},{lo});
out center;";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var resposta = await _http.PostAsync(url, content);
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
                string endereco = "Endereço não informado";

                if (el.TryGetProperty("tags", out var tags))
                {
                    if (tags.TryGetProperty("name", out var n))
                        nome = n.GetString() ?? nome;

                    string rua = "";
                    string numero = "";
                    string bairro = "";
                    string cidade = "";
                    string cep = "";

                    if (tags.TryGetProperty("addr:street", out var street))
                        rua = street.GetString() ?? "";

                    if (tags.TryGetProperty("addr:housenumber", out var houseNumber))
                        numero = houseNumber.GetString() ?? "";

                    if (tags.TryGetProperty("addr:suburb", out var suburb))
                        bairro = suburb.GetString() ?? "";

                    if (tags.TryGetProperty("addr:city", out var city))
                        cidade = city.GetString() ?? "";

                    if (tags.TryGetProperty("addr:postcode", out var postcode))
                        cep = postcode.GetString() ?? "";

                    var partes = new List<string>();

                    if (!string.IsNullOrWhiteSpace(rua))
                    {
                        if (!string.IsNullOrWhiteSpace(numero))
                            partes.Add($"{rua}, {numero}");
                        else
                            partes.Add(rua);
                    }

                    if (!string.IsNullOrWhiteSpace(bairro))
                        partes.Add(bairro);

                    if (!string.IsNullOrWhiteSpace(cidade))
                        partes.Add(cidade);

                    if (!string.IsNullOrWhiteSpace(cep))
                        partes.Add($"CEP {cep}");

                    if (partes.Count > 0)
                        endereco = string.Join(" - ", partes);
                }

                lista.Add(new Petshop
                {
                    Nome = nome,
                    Endereco = endereco,
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
