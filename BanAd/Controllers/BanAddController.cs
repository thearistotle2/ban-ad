using Microsoft.AspNetCore.Mvc;

namespace BanAd.Controllers;

public class BanAdController : Controller
{

    public IActionResult NewId()
    {
        var id = $"{Guid.NewGuid().GetHashCode():X}";
        return Content(id);
    }

    public IActionResult Ad(string id)
    {
        return PhysicalFile("/home/kevin/RiderProjects/ban-ad/BanAd/TempImages/629B5C2E.png", "image/png");
    }

    public IActionResult Out(string id)
    {
        return Redirect("https://waxp.rentals");
    }

    public IActionResult Advertise(string id)
    {
        return Content("");
    }
    
}