using Microsoft.AspNetCore.Mvc;

using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Data;

namespace ClearanceSystem.Controllers
{
    public class AccountController : Controller
    {
        SqlConnection db;


        public AccountController(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
           
        }
        public IActionResult Login()
        {

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(string Username, string Password)
        {

            var result = db.QueryFirstOrDefault("sp_clearance_login", new { Username, Password }, commandType: CommandType.StoredProcedure);



            if (result is null)

            {
                TempData["error"] = "INVALID USERNAME OR PASSWORD";
                TempData["invalid"] = "is-invalid";
                return RedirectToAction("Login", "Account");

            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, result.Name ?? ""));
            claims.Add(new Claim("position", result.position ?? ""));
            claims.Add(new Claim("username", result.UserName?.ToString() ?? ""));
            claims.Add(new Claim("department", result.depName ?? ""));
            claims.Add(new Claim("eid", result.eid ?? ""));
            claims.Add(new Claim("DepartmentId", result.Department?.ToString() ?? ""));


            if (result.eid == "09930"|| result.eid == "07826")
            {
                claims.Add(new Claim("HumanResources", "HumanResources"));
                claims.Add(new Claim("Admin", "Admin"));

            }
            if (result.eid != "09930" && result.eid != "07826")
            {
                claims.Add(new Claim("User", "User"));
            }

                // Add claims for CBC and CBR if true
                if (result.cid == 2)
            {
                claims.Add(new Claim("Company", "CBC"));
            }

            if (result.cid == 3)
            {
                claims.Add(new Claim("Company", "CBR"));
            }
            if (result.cid == 4)
            {
                claims.Add(new Claim("Company", "NVO"));
            }
            if (result.Audit == 1)
            {
                claims.Add(new Claim("Audit", "Audit"));
            }



            var roles = await db.QueryAsync("SELECT * FROM user_role where SystemName = 'Clearance' and EId = @Username", new { Username});
            foreach (var row in roles) 
            {

                claims.Add(new Claim(ClaimTypes.Role, row.Roles));

            }




            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };






        

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                // If either HumanResources or Training is 1, redirect to the "Index" action in the "Home" controller
                return RedirectToAction("Index", "Home");
             


        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);


            return RedirectToAction("Index", "Home");
        }

    }
}
