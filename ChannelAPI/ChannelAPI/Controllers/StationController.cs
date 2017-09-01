using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ChannelAPI.Models;
using ChannelAPI.Repositories;

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class StationController : Controller
    {
        private IConfiguration _config;
        public StationController(IConfiguration config)
        {
            this._config = config;
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var stationRepo = new StationRepository(this._config);
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
            var stationRepo = new StationRepository(this._config);
            var response = await stationRepo.FindByIDAsync(id);
            if (response == null)
                return NotFound();

            return Json(response);
        }

        [HttpPut("{fiosid}/logo/{bitmapid}")]
        public async Task<IActionResult> PutBitmap(string fiosid, int bitmapid, [FromBody] Image logo)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                var stationRepo = new StationRepository(this._config);
                await bitmapRepo.UpdateBitmap(logo, bitmapid.ToString());
                var response = await bitmapRepo.UpdateChannelBitmap(bitmapid);
                response += await stationRepo.UpdateBitmap(fiosid, bitmapid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
