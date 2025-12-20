using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;

namespace QDG.Migration.Web.Controllers;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var token = await _userService.LoginAsync(username, password);

        if (token == null)
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }

        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Username and password are required";
            return View();
        }

        var user = await _userService.RegisterAsync(username, password);

        if (user == null)
        {
            ViewBag.Error = "Username already exists";
            return View();
        }

        return RedirectToAction("Login");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }
}
