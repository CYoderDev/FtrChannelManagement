using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ChannelAPI.Models;

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class StationController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {

            var connection = await DapperFactory.GetOpenConnectionAsync();
            var response = await DapperFactory.QueryAsync<StationDTO>(connection, "SELECT * FROM [FIOSAPP_DC].[dbo].[tFIOSStation]");
            if (response == null || response.Count() <= 0)
            {
                return NotFound();
            }

            return Json(response);
        }
    }
}
