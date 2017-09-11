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

        /// <summary>
        /// Get all bitmap id's in the logo repository.
        /// </summary>
        /// <returns>int[]</returns>
        /// <example>GET: api/channelogo</example>
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

        /// <summary>
        /// Gets the image by bitmap id
        /// </summary>
        /// <param name="id">Channel logo bitmap id</param>
        /// <returns>image/png</returns>
        /// <example>GET: api/channellogo/2202</example>
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

        /// <summary>
        /// Gets all fios station id's that are currently assigned to the provided
        /// bitmap id.
        /// </summary>
        /// <param name="id">Channel logo bitmap id</param>
        /// <returns>int[]</returns>
        /// <example>GET: api/channellogo/2202/station</example>
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

        /// <summary>
        /// Inserts new logo image with a new bitmap id.
        /// </summary>
        /// <param name="value">Channe logo System.Drawing.Image</param>
        /// <param name="id">Channel logo bitmap id</param>
        /// <returns></returns>
        /// <example>POST: api/channellogo/2202</example>
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

        /// <summary>
        /// Updates the datetime fields for this bitmap id in order to
        /// prompt the STB to download the image.
        /// </summary>
        /// <param name="id">Channel logo bitmap id</param>
        /// <returns></returns>
        /// <example>PUT: api/channellogo/2202</example>
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

        /// <summary>
        /// Updates the logo image with a specific bitmap id, and the datetime
        /// fields in order to prompt the STB to download the new image.
        /// </summary>
        /// <param name="id">Logo bitmap id</param>
        /// <param name="value">System.Drawing.Image from request body</param>
        /// <returns></returns>
        /// <example>PUT: api/channellogo/image/2202</example>
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

        /// <summary>
        /// Assigns station to a new channel logo id
        /// </summary>
        /// <param name="bitmapid">Fios Channel Logo Id</param>
        /// <param name="fiosid">Fios Service Id</param>
        /// <returns></returns>
        /// <example>PUT: api/channellogo/2202/station/5</example>
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

        /// <summary>
        /// Deletes a logo image from the channel logo repository.
        /// </summary>
        /// <param name="id">Channel logo bitmap id</param>
        /// <returns></returns>
        /// <example>DELETE: api/channellogo/2202</example>
        [Authorize(policy: "RequireWindowsGroupMembership")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                this._bitmapRepo.DeleteBitmap(id.ToString());
                return Ok();
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning("File not found for bitmap id {0}. {1}{3}{2}", id, ex.Message, ex.StackTrace, System.Environment.NewLine);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
