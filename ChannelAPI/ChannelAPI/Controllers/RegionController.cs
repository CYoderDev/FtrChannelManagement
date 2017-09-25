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

        /// <summary>
        /// Get all active regions based on the application configuration
        /// </summary>
        /// <returns>string values in json format</returns>
        /// <example>/api/region/active</example>
        [AllowAnonymous]
        [HttpGet("active")]
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

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return NotFound();
        }

        [HttpPost]
        public IActionResult Post([FromBody]string value)
        {
            return NotFound();
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]string value)
        {
            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return NotFound();
        }
    }
}
