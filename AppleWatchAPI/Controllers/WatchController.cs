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
        public string Get() 
        {
            return "PM Watch Api";
        }

        [HttpGet("{refreshToken}/{latitude}/{longitude}")]
        public object Get(string refreshToken, string latitude, string longitude)
        {
            _client = GetClient();

            //1. (Optionally)Refresh Access Token if it is expired

            var token = GetAccessToken(refreshToken);

            _client = GetClient(token.ToString());

            var result = new Result();

            //2. / parking / active – to get current sessions
            //http://parknow.preprod.parkmobile.nl/api/parking/active?supplierid=349

            result.Session = GetActiveSession();

            //if there are current sessions – we are just displaying them, if not:
            //3. / search / zones / with user’s location
            //http://parknow.preprod.parkmobile.nl/api/search/zones?supplierid=349&lat=50.93847&maxresults=5&reverseGeocoder=true&lon=7.00743&radius=5

            result.Zones = GetNearbyZones(latitude, longitude);

            //4. / vehicles /
            //get users vehicles and assume the last one
            //http://parknow.preprod.parkmobile.nl/api/account/vehicles?supplierid=349

            result.Vehicles = GetVehicles();

            //5. / vehicles / lastused
            //get user’s last vehicle
            //http://parknow.preprod.parkmobile.nl/api/account/vehicles/lastused?supplierid=349

            result.VehicleLastUsed = GetLastVehicle();

            //6. / zone /
            //get zone details   for the first zone frome the nearbyzones list
            //http://parknow.preprod.parkmobile.nl/api/parking/zone/500003?supplierid=349

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
