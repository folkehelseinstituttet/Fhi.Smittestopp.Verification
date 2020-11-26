using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Fhi.Smittestopp.Verification.Server.Controllers
{
    public class DebugController : Controller
    {
        public string Status()
        {
            return DateTime.UtcNow.ToString("o");
        }


        public Dictionary<string, string> Headers()
        {
            return HttpContext.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());
        }
    }
}
