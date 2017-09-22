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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class RegionController : Controller
    {
        #region PrivateFields
        private IConfiguration _config;
        private ILogger<StationController> _logger;
        private RegionRepository _regionRepo;
        #endregion PrivateFields

        public RegionController(IConfiguration config, ILogger<StationController> logger, ILoggerFactory loggerFactory)
        {
            this._config = config;
            this._logger = logger;
            this._regionRepo = new RegionRepository(config, loggerFactory);
        }

        /// <summary>
        /// Get all Fios Regions
        /// </summary>
        /// <returns>FiosRegion Json Format</returns>
        /// <example>api/region</example>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var response = await _regionRepo.GetAllAsync();

                if (response == null || !response.Any())
                {
                    return NoContent();
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
        /// Get all active vho's based on application configuration
        /// </summary>
        /// <returns>string values in json format</returns>
        /// <example>/api/region/vho</example>
        [AllowAnonymous]
        [HttpGet("vho")]
        public async Task<IActionResult> GetActiveVhos()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var response = await _regionRepo.GetActiveVHOs();
                if (response == null || !response.Any())
                    return NoContent();
                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("region/active")]
        public async Task<IActionResult> GetActiveRegions()
        {
            try
            {
                _logger.LogTrace("Begin. params: none");
                var response = await _regionRepo.GetActiveRegions();
                if (response == null || !response.Any())
                    return NoContent();
                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return NotFound();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
