using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Repositories;
using ChannelAPI.Models;


namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private IConfiguration _config;

        public ChannelController(IConfiguration config)
        {
            this._config = config;
        }

        // GET: api/channel/Region/{regionid}
        [HttpGet("region/{id}")]
        public async Task<IActionResult> GetByRegion(string id)
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetByRegionAsync(id);
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        // Get api/vho/{id}
        [HttpGet("vho/{id}")]
        public async Task<IActionResult> GetByVHOId(string id)
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channel = await channelRepo.GetByVHOId(id);
                if (channel == null)
                    return NoContent();
                else
                    return Json(channel);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        // Get api/channel/station/{name}
        [HttpGet("station/{name}")]
        public async Task<IActionResult> GetByStationName(string name)
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetLikeColumn(name, "strStationName");
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        // Get api/channel/callsign/{name}
        [HttpGet("callsign/{name}")]
        public async Task<IActionResult> GetByCallSign(string name)
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetLikeColumn(name, "strStationCallSign");
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        // GET api/channel
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channels = await channelRepo.GetAllIdsAsync();
                if (!channels.Any())
                    return NoContent();
                else
                    return Json(channels);
            }
            catch (Exception ex)
            {
                //LogException
                return StatusCode(500);
            }
        }

        // GET api/channel/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                var channelRepo = new ChannelRepository(this._config);
                var channel = await channelRepo.FindByIDAsync(id);
                if (channel == null)
                    return NotFound();
                else
                    return Json(channel);
            }
            catch(Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
