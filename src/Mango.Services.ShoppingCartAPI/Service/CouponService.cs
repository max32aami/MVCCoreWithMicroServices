using Mango.Services.ShoppingCartAPI.Models.DTO;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Newtonsoft.Json;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class CouponService : ICouponService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public CouponService(IHttpClientFactory clientFactory)
        {
            _httpClientFactory = clientFactory;
        }
        public async Task<CouponDTO> GetCouponAsync(string CouponCode)
        {
            var client = _httpClientFactory.CreateClient("Coupon");
            var response = await client.GetAsync($"/api/coupon/GetByCode/{CouponCode}");
            var apiContet = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<ResponseDTO>(apiContet);
            if (resp.IsSucess)
            {
                return JsonConvert.DeserializeObject<CouponDTO>(Convert.ToString(resp.Result));
            }
            return new CouponDTO();
        }
    }
}
