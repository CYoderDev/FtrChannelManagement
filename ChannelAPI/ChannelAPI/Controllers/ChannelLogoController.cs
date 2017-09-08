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
using NLog.Extensions.Logging;
using ChannelAPI.Repositories;

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChannelLogoController : Controller
    {
        private IConfiguration _config;
        private ILogger<ChannelLogoController> _logger;
        private BitmapRepository _bitmapRepo;
        private StationRepository _stationRepo;

        public ChannelLogoController(IConfiguration config, ILogger<ChannelLogoController> logger, ILoggerFactory loggerFactory)
        {
            this._config = config;
            this._logger = logger;
            this._bitmapRepo = new BitmapRepository(config, loggerFactory);
            this._stationRepo = new StationRepository(config, loggerFactory);
        }

        // GET: api/channellogo
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogTrace("Begin. params: none");
            try
            {
                return await Task.FromResult<IActionResult>(Json(this._bitmapRepo.GetAllRepositoryIds()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // GET api/channellogo/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var img = this._bitmapRepo.GetBitmapById(id.ToString());
                return File(img, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpGet("{id}/station")]
        public async Task<IActionResult> GetStations(int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var stations = await this._bitmapRepo.GetStationsByBitmapId(id);
                if (!stations.Any())
                    return NoContent();
                return Json(stations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // POST api/channellogo/5
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPost("{id}")]
        public async Task<IActionResult> Post([FromBody]Image value, int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: id={0}", id);
                await this._bitmapRepo.InsertBitmap(value, id.ToString());
                await this._bitmapRepo.UpdateChannelBitmap(id, value);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/channellogo/Image/5
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut("image/{id}")]
        public async Task<IActionResult> Put([FromBody]Image value, int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: id={0}", id);
                int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
                if (id <= 0 || id > maxVal)
                {
                    _logger.LogWarning("Invalid ID. Must be greater than 0 and less than {0}. Provided value: {1}", maxVal, id);
                    return BadRequest();
                }
                await this._bitmapRepo.UpdateBitmap(value, id.ToString());
                int retVal = await this._bitmapRepo.UpdateChannelBitmap(id, value);
                
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/channellogo/5
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: id={0}", id);
                int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
                if (id <= 0 || id > maxVal)
                {
                    _logger.LogWarning("Invalid ID. Must be greater than 0 and less than {0}. Provided value: {1}", maxVal, id);
                    return BadRequest();
                }
                await this._bitmapRepo.UpdateChannelBitmap(id);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT: api/channellogo/Image/5
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut("image/{id}")]
        public async Task<IActionResult> Put (int id, [FromBody]Image value)
        {
            try
            {
                _logger.LogTrace("Begin. params: id={0}", id);
                int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
                if (id <= 0 || id > maxVal)
                {
                    _logger.LogWarning("Invalid ID. Must be greater than 0 and less than {0}. Provided value: {1}", maxVal, id);
                    return BadRequest();
                }
                using (value)
                    await this._bitmapRepo.UpdateBitmap(value, id.ToString());
                var retVal = await this._bitmapRepo.UpdateChannelBitmap(id);
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpPut("{bitmapid}/Station/{fiosid}")]
        public async Task<IActionResult> Put (int bitmapid, string fiosid)
        {
            try
            {
                _logger.LogTrace("Begin. params: bitmapid={0}, fiosid={1}", bitmapid, fiosid);
                int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
                if (bitmapid <= 0 || bitmapid > maxVal)
                {
                    _logger.LogWarning("Invalid ID. Must be greater than 0 and less than {0}. Provided value: {1}", maxVal, bitmapid);
                    return BadRequest();
                }
                var retVal = await _stationRepo.UpdateBitmap(fiosid, bitmapid);
                return Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/values/5
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
