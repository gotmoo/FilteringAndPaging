using System.Diagnostics;
using System.Web;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using EucRepo.Models;
using EucRepo.ModelsView;
using EucRepo.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Controllers;

public class HomeController : Controller
{

 
    [Authorize]
    public async Task<IActionResult> Index()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
