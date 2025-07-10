using ClearanceSystem.Models;
using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

    }
}
