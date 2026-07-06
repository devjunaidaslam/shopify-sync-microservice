namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class MetaFields
    {
        public int Id { get; set; }
        public long OwnerId {  get; set; }
        public string OwnerType { get; set; }
        public string NameSpace { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
