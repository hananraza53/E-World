using System.Collections.Generic;

namespace project26.Models
{
    public class AdminDashboardViewModel
    {
        public List<User> RecentUsers { get; set; } = new List<User>();
        
        public int TotalUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int UnverifiedUsers { get; set; }
        public int AdminUsers { get; set; }

        public string UserGrowth { get; set; } = "0%";
        public List<int> UserGrowthData { get; set; } = new List<int>();
        public List<string> ChartLabels { get; set; } = new List<string>();
    }
}
