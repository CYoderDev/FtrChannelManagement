using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ChannelAPI.Models;
using ChannelAPI.Repositories;

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class StationController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var stationRepo = new StationRepository();
            var response = await stationRepo.GetAllAsync();
            if (response == null || response.Count() <= 0)
            {
                return NotFound();
            }

            return Json(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var stationRepo = new StationRepository();
            var response = await stationRepo.FindByIDAsync(id);
            if (response == null)
                return NotFound();

            return Json(response);
        }
    }
}
