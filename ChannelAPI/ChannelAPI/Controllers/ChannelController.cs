using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Repositories;
using ChannelAPI.Models;


namespace ChannelAPI.Controllers
{
    [Authorize(policy: "RequireWindowsGroupMembership")]
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        #region PrivateFields
        private IConfiguration _config;
        private ILogger<ChannelController> _logger;
        private ChannelRepository _channelRepo;
        #endregion PrivateFields


        #region Constructor
        public ChannelController(IConfiguration config, ILogger<ChannelController> logger)
        {
            this._config = config;
            this._logger = logger;
            this._channelRepo = new ChannelRepository(config);
        }
        #endregion Constructor

        #region GET
        /// <summary>
        /// Get all channel information by region id
        /// </summary>
        /// <param name="id">FiOS region ID</param>
        /// <returns>FiOS Channel[]</returns>
        /// <example>GET: api/channel/region/93636</example>
        [AllowAnonymous]
        [HttpGet("region/{id}")]
        public async Task<IActionResult> GetByRegion(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channels = await _channelRepo.GetByRegionAsync(id);
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all channel information by Genre ID
        /// </summary>
        /// <param name="id">ID value of the genre</param>
        /// <returns>FiOS Channel[]</returns>
        /// <example>GET: api/channel/genre/2</example>
        [AllowAnonymous]
        [HttpGet("genre/{id}")]
        public async Task<IActionResult> GetByGenre(int id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var genre = await _channelRepo.GetByGenreId(id);
                return Json(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all channel information by VHO id
        /// </summary>
        /// <param name="id">FiOS VHO ID</param>
        /// <returns>FiOS Channel[]</returns>
        /// <example>GET: api/channel/vho/1</example>
        [AllowAnonymous]
        [HttpGet("vho/{id}")]
        public async Task<IActionResult> GetByVHOId(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channel = await _channelRepo.GetByVHOId(id);
                if (channel == null)
                {
                    _logger.LogDebug("No channels found.");
                    return NoContent();
                }
                else
                {
                    _logger.LogDebug("{0} channels returned.", channel.Count());
                    return Json(channel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get channel by station name.
        /// </summary>
        /// <param name="name">All or part of the FiOS station name</param>
        /// <returns>FiOS Channel[]</returns>
        /// <example>GET: api/channel/station/abc</example>
        [AllowAnonymous]
        [HttpGet("station/{name}")]
        public async Task<IActionResult> GetByStationName(string name)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", name);
                var channels = await _channelRepo.GetLikeColumn(name, "strStationName");
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get channel information by station call sign
        /// </summary>
        /// <param name="name">Station call sign</param>
        /// <returns>Fios Channel[]</returns>
        /// <example>GET: api/channel/callsign/abchd</example>
        [AllowAnonymous]
        [HttpGet("callsign/{name}")]
        public async Task<IActionResult> GetByCallSign(string name)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", name);
                var channels = await _channelRepo.GetLikeColumn(name, "strStationCallSign");
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets all active FiOS service id's
        /// </summary>
        /// <returns>int[]</returns>
        /// <example>GET: api/channel</example>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var channels = await _channelRepo.GetAllIdsAsync();
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get channel by FiOS service ID
        /// </summary>
        /// <param name="id">FiOS service ID</param>
        /// <returns>FiOS channel</returns>
        /// <example>GET: api/channel/5</example>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channels = await _channelRepo.FindAllByIDAsync(id);
                if (!channels.Any())
                    return NotFound();
                else
                    return Json(channels);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        #endregion GET
    }
}
