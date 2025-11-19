using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Semester03.Areas.Client.Models.Vnpay;
using Semester03.Libraries;

namespace Semester03.Services.Vnpay
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VnPayLibrary();

            // read callback with fallback keys
            var urlCallBack = _configuration["PaymentCallBack:ReturnUrl"];
            if (string.IsNullOrWhiteSpace(urlCallBack))
            {
                urlCallBack = _configuration["Vnpay:ReturnUrl"]
                           ?? _configuration["Vnpay:PaymentBackReturnUrl"]
                           ?? _configuration["Vnpay:PaymentBackReturnURL"];
            }
            if (string.IsNullOrWhiteSpace(urlCallBack))
            {
                // fallback to inferred URL from current request (dev only)
                urlCallBack = $"{context.Request.Scheme}://{context.Request.Host}/Client/Booking/PaymentCallbackVnpay";
                Console.WriteLine("Warning: Using inferred VNPAY return URL: " + urlCallBack);
            }

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);

            // Ensure amount format: VNPAY often expects amount * 100 (smallest unit).
            // model.Amount expected as whole VND (e.g. 240000)
            try
            {
                var vnpAmountLong = Convert.ToInt64(Math.Round((decimal)model.Amount * 100m, 0, MidpointRounding.AwayFromZero));
                pay.AddRequestData("vnp_Amount", vnpAmountLong.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error formatting vnp_Amount: " + ex);
                // fallback: send plain integer VND
                var fallback = Convert.ToInt64(Math.Round((decimal)model.Amount, 0));
                pay.AddRequestData("vnp_Amount", fallback.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);

            var orderInfo = $"{model.Name} {model.OrderDescription}".Trim();
            pay.AddRequestData("vnp_OrderInfo", orderInfo);
            pay.AddRequestData("vnp_OrderType", model.OrderType);

            // add ReturnUrl
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);

            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);

            // Debug logging
            Console.WriteLine("=== VNPAY LIB REQUESTDATA ===");
            Console.WriteLine($"vnp_Amount = {pay.GetType() /* can't read private SortedList here, but CreateRequestUrl printed query earlier */}");
            Console.WriteLine("QueryString (for sign): " + paymentUrl.Substring(0, Math.Min(paymentUrl.Length, 1000)) + " ...");
            Console.WriteLine("VnPayService returned URL:");
            Console.WriteLine(paymentUrl);

            return paymentUrl;
        }


        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

            return response;
        }
    }
}