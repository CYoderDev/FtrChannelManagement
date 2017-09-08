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
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private IConfiguration _config;
        private ILogger<ChannelController> _logger;
        private ChannelRepository _channelRepo;

        public ChannelController(IConfiguration config, ILogger<ChannelController> logger)
        {
            this._config = config;
            this._logger = logger;
            this._channelRepo = new ChannelRepository(config);
        }

        // GET: api/channel/Region/{regionid}
        [HttpGet("region/{id}")]
        public async Task<IActionResult> GetByRegion(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetByRegionAsync(id);
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

        // GET: api/vho/{id}
        [HttpGet("vho/{id}")]
        public async Task<IActionResult> GetByVHOId(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channelRepo = new ChannelRepository(this._config);
                var channel = await channelRepo.GetByVHOId(id);
                if (channel == null)
                    return NoContent();
                else
                    return Json(channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // GET: api/channel/station/{name}
        [HttpGet("station/{name}")]
        public async Task<IActionResult> GetByStationName(string name)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", name);
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetLikeColumn(name, "strStationName");
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

        // GET: api/channel/callsign/{name}
        [HttpGet("callsign/{name}")]
        public async Task<IActionResult> GetByCallSign(string name)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", name);
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetLikeColumn(name, "strStationCallSign");
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

        // GET: api/channel
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetAllIdsAsync();
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

        // GET: api/channel/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                _logger.LogTrace("Begin. params: {0}", id);
                var channelRepo = new ChannelRepository(this._config);
                var channel = await channelRepo.FindByIDAsync(id);
                if (channel == null)
                    return NotFound();
                else
                    return Json(channel);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
