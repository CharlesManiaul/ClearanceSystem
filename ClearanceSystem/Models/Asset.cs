namespace ClearanceSystem.Models
{
    public class Asset
    {
        public int AId { get; set; }
        public string OwnerName { get; set; }
        public string Location { get; set; }
        public string AssetNo { get; set; }
        public string SerialNo { get; set; }
        public string Description { get; set; }
        public string Specs { get; set; }
        public bool IsSelected { get; set; }
    }
}
