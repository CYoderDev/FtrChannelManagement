using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ChannelAPI.Repositories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChannelAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChannelLogoController : Controller
    {
        private IConfiguration _config;

        public ChannelLogoController(IConfiguration config)
        {
            this._config = config;
        }

        // GET: api/channellogo
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var bitmapRepo = new BitmapRepository(this._config);

            return await Task.FromResult<IActionResult>(Json(bitmapRepo.GetAllIds()));
        }

        // GET api/channellogo/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var bitmapRepo = new BitmapRepository(this._config);
            var img = await bitmapRepo.GetBitmapById(id.ToString());
            return File(img, "image/png");
        }

        // POST api/channellogo/5
        [HttpPost("{id}")]
        public async Task<IActionResult> Post([FromBody]Image value, int id)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                await bitmapRepo.InsertBitmap(value, id.ToString());
                await bitmapRepo.UpdateChannelBitmap(id, null, value);
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT api/channellogo/5/1224
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromBody]Image value, int id)
        {
            int maxVal = int.Parse(this._config.GetValue<string>("FiosChannelData:DefaultLogoId"));
            if (id <= 0 || id > maxVal)
                return BadRequest();
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                await bitmapRepo.UpdateBitmap(value, id.ToString());
                await bitmapRepo.UpdateChannelBitmap(id, null, value);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpPut("{oldid}/{newid}")]
        public async Task<IActionResult> Put(int oldid, int newid)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                using (var valueStream = await bitmapRepo.GetBitmapById(newid.ToString()))
                {
                    await bitmapRepo.UpdateChannelBitmap(oldid, newid, Image.FromStream(valueStream));
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpPut("Image/{oldid}/{newid}")]
        public async Task<IActionResult> Put (int oldid, int newid, [FromBody]Image value)
        {
            try
            {
                var bitmapRepo = new BitmapRepository(this._config);
                await bitmapRepo.UpdateBitmap(value, newid.ToString());
                await bitmapRepo.UpdateChannelBitmap(oldid, newid, value);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
