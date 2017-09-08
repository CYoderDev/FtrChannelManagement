using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChannelAPI.Models;
using ChannelAPI.Repositories;

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class StationController : Controller
    {
        private IConfiguration _config;
        private ILogger<StationController> _logger;
        private StationRepository _stationRepo;
        private BitmapRepository _bitmapRepo;

        public StationController(IConfiguration config, ILogger<StationController> logger, ILoggerFactory loggerFactory)
        {
            this._config = config;
            this._logger = logger;
            this._stationRepo = new StationRepository(config, loggerFactory);
            this._bitmapRepo = new BitmapRepository(config, loggerFactory);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var response = await _stationRepo.GetAllAsync();
                if (response == null || response.Count() <= 0)
                {
                    return NotFound();
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var response = await _stationRepo.FindByIDAsync(id);
                if (response == null)
                    return NotFound();

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]FiosStation station)
        {
            try
            {
                _logger.LogTrace("Begin. params: StationID= {0}", station.strFIOSServiceId);
                if (!ModelState.IsValid)
                {
                    _logger.LogError("Model State is not valid. Bad request.");
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                await _stationRepo.UpdateAsync(station);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut("{fiosid}/logo/{bitmapid}")]
        public async Task<IActionResult> PutBitmap(string fiosid, int bitmapid, [FromBody] Image logo)
        {
            try
            {
                _logger.LogTrace("Begin. params: fiosid={0}, bitmapid={1}, logo={2}", fiosid, bitmapid, logo != null);
                await _bitmapRepo.UpdateBitmap(logo, bitmapid.ToString());
                var response = await _bitmapRepo.UpdateChannelBitmap(bitmapid);
                response += await _stationRepo.UpdateBitmap(fiosid, bitmapid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
