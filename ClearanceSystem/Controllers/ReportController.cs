using ClearanceSystem.Models;
using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ClearanceSystem.Controllers
{
    public class ReportController : Controller
    {

        SqlConnection db, adb;


        public ReportController(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            adb = new SqlConnection(configuration.GetConnectionString("AssetConnection"));

        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AuditTrail()
        {
            Reports report = new Reports();


            report.report = db.Query<Reports>(@"SELECT * FROM AuditTrail 
                                                where [function] = 'Clearance'
                                                order by createdDate desc");


            return View(report);

        }
        public IActionResult OverDueReport()
        {
            Reports report = new Reports();

            // ✅ Get all roles for the logged-in user
            var roles = HttpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var UserId = HttpContext.User.FindFirstValue("username");

            // ✅ Option A: Pass as comma-separated string
            var Role = string.Join(",", roles);

            report.report = db.Query<Reports>(
                "sp_clr_OverDue_List",
                new { Role , UserId },
                commandType: CommandType.StoredProcedure);

            return View(report); 
        }





    }
}
