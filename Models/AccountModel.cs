using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Models
{
    public class AccountModel : DBModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public AccountModel(int id, string firstName, string lastName, string gender, string email, string password, string role) : base(id)
        {
            FirstName = firstName;
            LastName = lastName;
            Gender = gender;
            Email = email;
            Password = password;
            Role = role;
        }
        public AccountModel() : base(0) { }
    }
}
