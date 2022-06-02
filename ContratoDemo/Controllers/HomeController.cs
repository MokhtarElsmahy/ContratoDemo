using ContratoDemo.Models;
using ContratoDemo.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ContratoDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<FirstStepResponseBody> firstStep(HttpClient httpClient)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("https://accept.paymob.com/api/auth/tokens");
            request.Method = HttpMethod.Post;
            FirstStepRequestBody firstStepRequestBody = new FirstStepRequestBody();
            firstStepRequestBody.api_key = "ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TVRnMk1EY3lMQ0p1WVcxbElqb2lhVzVwZEdsaGJDSjkuNXhVWHZLTmh1RzFmRjFsczVYV05WY2NDZE9xMXY5Z0lKUTRWbnAwS0tvZWg0Q1dQeVliMEhRWFhPa01kcjZXSTU5UkxublZkYWRZYk1WY21UaHNiRmc=";

            request.Content = new StringContent(JsonConvert.SerializeObject(firstStepRequestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage Firstresponse = await httpClient.SendAsync(request);
            if (Firstresponse.IsSuccessStatusCode)
            {
                //first Step
                var FirstresponseString = await Firstresponse.Content.ReadAsStringAsync();
                FirstStepResponseBody firstStepResponseBody = JsonConvert.DeserializeObject<FirstStepResponseBody>(FirstresponseString);
                if (!string.IsNullOrEmpty(firstStepResponseBody.token))
                {
                    return firstStepResponseBody;
                }

            }
            return new FirstStepResponseBody() { token = string.Empty };
        }


        public async Task<SecondResponseBody> SecondStep(HttpClient httpClient, FormViewModel model, int TotalIntegerAmountRequired, int ItemIntegerAmountRequired, string token)
        {
            HttpRequestMessage Secondrequest = new HttpRequestMessage();
            Secondrequest.RequestUri = new Uri("https://accept.paymob.com/api/ecommerce/orders");
            Secondrequest.Method = HttpMethod.Post;
            SecondStepRequestBody SecondStepRequestBody = new SecondStepRequestBody();


            SecondStepRequestBody.items = new List<Item> { new Item { name = model.ProductName, description = "default", quantity = model.Quantity.ToString(), amount_cents = ItemIntegerAmountRequired.ToString() } };
            SecondStepRequestBody.amount_cents = TotalIntegerAmountRequired.ToString();
            SecondStepRequestBody.auth_token = token;
            SecondStepRequestBody.delivery_needed = "false";
            SecondStepRequestBody.currency = "EGP";
            Secondrequest.Content = new StringContent(JsonConvert.SerializeObject(SecondStepRequestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage SecondStepResponse = await httpClient.SendAsync(Secondrequest);
            if (SecondStepResponse.IsSuccessStatusCode)
            {
                var SecondresponseString = await SecondStepResponse.Content.ReadAsStringAsync();
                SecondResponseBody SecondResponseBody = JsonConvert.DeserializeObject<SecondResponseBody>(SecondresponseString);
                if (SecondResponseBody.id != 0)
                {
                    return SecondResponseBody;

                }

            }
            return new SecondResponseBody();
        }

        public async Task<ThirdStepResponse> ThirdStep(HttpClient httpClient, FormViewModel model, string TotalIntegerAmountRequired, string id, string token)
        {
            HttpRequestMessage Thirdrequest = new HttpRequestMessage();
            Thirdrequest.RequestUri = new Uri("https://accept.paymob.com/api/acceptance/payment_keys");
            Thirdrequest.Method = HttpMethod.Post;
            ThirdStepRequestBody thirdStepRequestBody = new ThirdStepRequestBody();
            thirdStepRequestBody.integration_id = 2171673;
            thirdStepRequestBody.order_id = id;
            thirdStepRequestBody.auth_token = token;
            thirdStepRequestBody.expiration = 3600;
            thirdStepRequestBody.currency = "EGP";
            thirdStepRequestBody.amount_cents = TotalIntegerAmountRequired;
            thirdStepRequestBody.lock_order_when_paid = "false";
            StringBuilder phone = new StringBuilder();
            phone.Append($"+2{model.phone_number}");
            thirdStepRequestBody.billing_data = new BillingData
            {
                phone_number = phone.ToString(),
                email = model.email,
                first_name = model.first_name,
                last_name = model.last_name,
                apartment = "NA",
                floor = "NA",
                street = "NA",
                building = "NA",
                shipping_method = "NA",
                postal_code = "NA",
                city = "NA",
                country = "NA",
                state = "NA"
            };

            Thirdrequest.Content = new StringContent(JsonConvert.SerializeObject(thirdStepRequestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage ThirdStepResponse = await httpClient.SendAsync(Thirdrequest);
            if (ThirdStepResponse.IsSuccessStatusCode)
            {
                var ThirdresponseString = await ThirdStepResponse.Content.ReadAsStringAsync();
                ThirdStepResponse ThirdResponseBody = JsonConvert.DeserializeObject<ThirdStepResponse>(ThirdresponseString);
                if (!string.IsNullOrEmpty(ThirdResponseBody.token))
                {
                    return ThirdResponseBody;
                }

            }
            return new ThirdStepResponse();
        }

        [HttpPost]
        public async Task<IActionResult> Index(FormViewModel model)
        {
            HttpClient httpClient = new HttpClient();
            var TotalIntegerAmountRequired = ((model.Quantity * model.Price)) * 100;
            var ItemIntegerAmountRequired = (model.Price) * 100;


            FirstStepResponseBody firstStepResponseBody = await firstStep(httpClient);// JsonConvert.DeserializeObject<FirstStepResponseBody>(FirstresponseString);


            SecondResponseBody SecondResponseBody = await SecondStep(httpClient, model, TotalIntegerAmountRequired, ItemIntegerAmountRequired, firstStepResponseBody.token);//JsonConvert.DeserializeObject<SecondResponseBody>(SecondresponseString);


            ThirdStepResponse ThirdResponseBody = await ThirdStep(httpClient, model, TotalIntegerAmountRequired.ToString(), SecondResponseBody.id.ToString(), firstStepResponseBody.token);

            if (!string.IsNullOrEmpty(ThirdResponseBody.token))
            {
                string ifram = $"https://accept.paymob.com/api/acceptance/iframes/391544?payment_token={ThirdResponseBody.token}";
                return Redirect(ifram);
            }

            return View();
        }




       

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}