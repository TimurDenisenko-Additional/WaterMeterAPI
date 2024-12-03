using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Models
{
    public class WaterMeterModel(int id, string email, string address, int apartment, DateTime date, int coldWater, int warmWater, bool paymentStatus) : DBModel(id)
    {
        public string Email { get; set; } = email;
        public string Address { get; set; } = address;
        public int Apartment { get; set; } = apartment;
        public DateTime Date { get; set; } = date;
        public int ColdWater { get; set; } = coldWater;
        public int WarmWater { get; set; } = warmWater;
        public bool PaymentStatus { get; set; } = paymentStatus;
    }
}
