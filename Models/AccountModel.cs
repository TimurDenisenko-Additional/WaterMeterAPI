using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Models
{
    public class AccountModel(int id, string firstName, string lastName, string gender, string email, string password, string role) : DBModel(id)
    {
        public string FirstName { get; set; } = firstName;
        public string LastName { get; set; } = lastName;
        public string Gender { get; set; } = gender;
        public string Email { get; set; } = email;
        public string Password { get; set; } = password;
        public string Role { get; set; } = role;
    }
}
