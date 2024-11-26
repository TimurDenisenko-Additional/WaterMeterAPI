namespace WaterMeterAPI.Models.DB
{
    public abstract class DBModel
    {
        public int Id { get; set; }
        public DBModel(int id) 
        {
            Id = id;
        }
    }
}
