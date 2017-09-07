using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChannelAPI.Repositories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChannelLogoController : Controller
    {
        private IConfiguration _config;
        private ILogger _logger;

        public ChannelLogoController(IConfiguration config, ILoggerFactory loggerFactory)
        {
            this._config = config;
            this._logger = loggerFactory.CreateLogger<ChannelLogoController>();
        }

        // GET: api/channellogo
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var bitmapRepo = new BitmapRepository(this._config);

            return await Task.FromResult<IActionResult>(Json(bitmapRepo.GetAllRepositoryIds()));
        }

        // GET api/channellogo/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var bitmapRepo = new BitmapRepository(this._config);
            var img = bitmapRepo.GetBitmapById(id.ToString());
            return File(img, "image/png");
        }

        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpGet("{id}/station")]
        public async Task<IActionResult> GetStations(int id)
        {
            try
            {
                this._logger.LogDebug("GET: ChannelLogoController:GetStations({0})", id);
                var bitmapRepo = new BitmapRepository(this._config);
                var stations = await bitmapRepo.GetStationsByBitmapId(id);
                if (!stations.Any())
                    return NoContent();
                return Json(stations);
            }
            catch (Exception ex)
            {
                this._logger.LogError(new EventId(502), ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // POST api/channellogo/5
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpPost("{id}")]
        public async Task<IActionResult> Post([FromBody]Image value, int id)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                await bitmapRepo.InsertBitmap(value, id.ToString());
                await bitmapRepo.UpdateChannelBitmap(id, value);
                return Ok();
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/channellogo/Image/5
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpPut("image/{id}")]
        public async Task<IActionResult> Put([FromBody]Image value, int id)
        {
            int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
            if (id <= 0 || id > maxVal)
                return BadRequest();
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                await bitmapRepo.UpdateBitmap(value, id.ToString());
                int retVal = await bitmapRepo.UpdateChannelBitmap(id, value);
                
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/channellogo/5
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);

                await bitmapRepo.UpdateChannelBitmap(id);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT: api/channellogo/Image/5
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpPut("image/{id}")]
        public async Task<IActionResult> Put (int id, [FromBody]Image value)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                using (value)
                    await bitmapRepo.UpdateBitmap(value, id.ToString());
                var retVal = await bitmapRepo.UpdateChannelBitmap(id);
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpPut("{bitmapid}/Station/{fiosid}")]
        public async Task<IActionResult> Put (int bitmapid, string fiosid)
        {
            try
            {
                var stationRepo = new StationRepository(this._config);
                var retVal = await stationRepo.UpdateBitmap(fiosid, bitmapid);
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/values/5
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
