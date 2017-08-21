using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using AppleWatchAPI.Models;

namespace AppleWatchAPI.Controllers
{
    [Route("/")]
    public class WatchController : Controller
    {
        private HttpClient _client;

        [HttpGet()]
        public string Get() => "PM Watch Api";

        [HttpGet("access/{accessToken}/{latitude}/{longitude}")]
        public Result GetWithAccessToken(string accessToken, string latitude, string longitude)
        {
            _client = GetClient(accessToken);

            var result = new Result { AccessToken = accessToken };

            return GetResult(result, latitude, longitude);
        }

        [HttpGet("refresh/{refreshToken}/{latitude}/{longitude}")]
        public Result GetWithRefreshToken(string refreshToken, string latitude, string longitude)
        {
            _client = GetClient();

            var accessToken = GetAccessToken(refreshToken);

            _client = GetClient(accessToken.ToString());

            var result = new Result() { AccessToken = accessToken };

            return GetResult(result, latitude, longitude);
        }

        private Result GetResult(Result result, string latitude, string longitude)
        {
            result.Session = GetActiveSession();
            result.Zones = GetNearbyZones(latitude, longitude);
            result.Vehicles = GetVehicles();
            result.VehicleLastUsed = GetLastVehicle();

            if (result.Zones[0]["internalZoneCode"] != null)
            {
                result.ZoneDetail = GetZone((result.Zones as JArray)[0]["internalZoneCode"].ToString());
            }

            return result;
        }

        private HttpClient GetClient(string token = "")
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("PMAuthenticationToken", token);
            return client;
        }

        private object GetAccessToken(string refreshToken)
        {
            var json = PostJson("http://parknow.preprod.parkmobile.nl/api/token/refresh", $"{{\"RefreshToken\":\"{refreshToken}\"}}");
            return JObject.Parse(json)["token"];
        }

        private object GetActiveSession()
        {
            var json = GetJson("http://parknow.preprod.parkmobile.nl/api/parking/active?supplierid=349");
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            return JObject.Parse(json);
        }

        private dynamic GetNearbyZones(string latitude, string longitude)
        {
            var json = GetJson($"http://parknow.preprod.parkmobile.nl/api/search/zones?supplierid=349&lat={latitude}&maxresults=5&reverseGeocoder=true&lon={longitude}&radius=5");
            return JObject.Parse(json)["zones"];
        }

        private object GetVehicles()
        {
            var json = GetJson("http://parknow.preprod.parkmobile.nl/api/account/vehicles?supplierid=349");
            return JObject.Parse(json)["vehicles"];
        }

        private object GetLastVehicle()
        {
            var json = GetJson("http://parknow.preprod.parkmobile.nl/api/account/vehicles/lastused?supplierid=349");
            return JObject.Parse(json)["vehicles"];
        }

        private object GetZone(string zoneCode)
        {
            var json = GetJson($"http://parknow.preprod.parkmobile.nl/api/parking/zone/{zoneCode}?supplierid=349");
            return JObject.Parse(json)["zones"];
        }

        private string GetJson(string url)
        {
            try
            {
                return _client.GetStringAsync(url).Result;
            }
            catch (System.Exception)
            {
                return string.Empty;
            }       
        }

        private string PostJson(string url, string body) => 
            _client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync().Result;
    }
}
