namespace ITAssetManager.Web.Models
{
    public class ChangeLog
    {
        public int Id { get; set; }
        public string Entity { get; set; } = "";   // "Asset","SoftwareProduct", etc.
        public string Key { get; set; } = "";      // primary key value
        public string Action { get; set; } = "";   // Insert/Update/Delete
        public string? UserName { get; set; }
        public DateTime At { get; set; } = DateTime.UtcNow;
        public string? ChangesJson { get; set; }   // optional: property diffs
    }

}
