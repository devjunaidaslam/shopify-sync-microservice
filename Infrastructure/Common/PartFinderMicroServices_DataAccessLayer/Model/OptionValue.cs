namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class OptionValue
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int OptionId { get; set; }
        public Option Option { get; set; }
    }
}
