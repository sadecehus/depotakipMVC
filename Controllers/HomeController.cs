using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pm.Data;
using pm.Models;

namespace pm.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        // Eğer zaten giriş yapmışsa dashboard'a yönlendir
        if (HttpContext.Session.GetString("UserId") != null)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // Veritabanından kullanıcı kontrolü
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        
        if (user != null)
        {
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            return RedirectToAction("Dashboard");
        }
        
        ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
        return View("Index");
    }

    public IActionResult Dashboard()
    {
        // Giriş kontrolü
        if (HttpContext.Session.GetString("UserId") == null)
        {
            return RedirectToAction("Index");
        }
        
        ViewBag.Username = HttpContext.Session.GetString("Username");
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
