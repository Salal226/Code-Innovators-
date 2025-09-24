namespace ITAssetManager.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalAssets { get; set; }
        public int InRepair { get; set; }
        public int ExpiringWarranty30 { get; set; }

        public int SoftwareProducts { get; set; }
        public int LicensesTotalSeats { get; set; }
        public int LicensesSeatsInUse { get; set; }
    }
}
