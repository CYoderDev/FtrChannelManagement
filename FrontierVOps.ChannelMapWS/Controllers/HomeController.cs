using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "FiOS UI";

            return View();
        }

        public ActionResult FileUploadView()
        {
            ViewBag.Title = "FiOS File Upload API";

            return View("FileUploadView");
        }
    }
}
