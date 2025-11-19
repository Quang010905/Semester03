namespace Semester03.Areas.Client.Models.ViewModels
{
    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string VnPayResponseCode { get; set; }
        public string OrderDescription { get; set; }
        public string TxnRef { get; set; }
    }
}