namespace WaterMeterAPI.Models.DB
{
    public abstract class DBModel(int id)
    {
        public int Id { get; set; } = id;
    }
}
