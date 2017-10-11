using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
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
        #region PrivateFields
        private IConfiguration _config;
        private ILogger<StationController> _logger;
        private StationRepository _stationRepo;
        private BitmapRepository _bitmapRepo;
        #endregion PrivateFields

        #region Constructor
        public StationController(IConfiguration config, ILogger<StationController> logger, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
        {
            this._config = config;
            this._logger = logger;
            this._stationRepo = new StationRepository(config, loggerFactory);
            this._bitmapRepo = new BitmapRepository(config, loggerFactory, hostingEnvironment);
        }
        #endregion Constructor

        #region GET
        /// <summary>
        /// Gets all FiOS stations
        /// </summary>
        /// <returns>FiOS station</returns>
        /// <example>GET: api/station</example>
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

        /// <summary>
        /// Get FiOS station by FiOS service ID
        /// </summary>
        /// <param name="id">FiOS service ID</param>
        /// <returns>FiOS station</returns>
        /// <example>GET: api/station/5</example>
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
        #endregion GET

        #region PUT
        /// <summary>
        /// Updates a FiOS station
        /// </summary>
        /// <param name="station">FiOS station with updated values from request body</param>
        /// <returns></returns>
        /// <example>PUT: api/station</example>
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
                station.dtLastUpdateDate = DateTime.Now;
                await _stationRepo.UpdateAsync(station);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates the channel logo and assigned it to a station
        /// </summary>
        /// <param name="fiosid">Fios Service Id for the station</param>
        /// <param name="bitmapid">New bitmap id to assign to the station</param>
        /// <param name="logo">System.Drawing.Image logo to update and assign to the station</param>
        /// <returns></returns>
        /// <example>PUT: api/station/5/logo/2202</example>
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
        #endregion PUT
    }
}
