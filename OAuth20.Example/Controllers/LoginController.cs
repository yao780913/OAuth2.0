using Microsoft.AspNetCore.Mvc;

namespace OAuth20.Example.Controllers;

public class LoginController : Controller
{
    public IActionResult Index ()
    {
        return View();
    }

}