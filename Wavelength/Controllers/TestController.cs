//using Microsoft.AspNetCore.Mvc;
//using Wavelength.Data;
//using Wavelength.Services;

//namespace Wavelength.Controllers
//{
//    [ApiController]
//    [Route("[controller]")]
//    public class TestController : BaseController
//    {
//        private readonly MailService mailService;

//        public TestController(AppDbContext dbContext, MailService mailService) : base(dbContext)
//        {
//            this.mailService = mailService;
//        }

//        [HttpGet("ViewTemplate")]
//        public async Task<ActionResult> ViewTemplate(string template, string name = "John Doe")
//        {
//            try
//            {
//                var html = mailService.RenderTemplate(template, new Dictionary<string, string>
//                {
//                    { "Name", name },
//                    { "Date", DateTime.Now.ToString("MMMM dd, yyyy") }
//                });
//                return Content(html.Text, "text/html");
//            }
//            catch (FileNotFoundException ex)
//            {
//                return NotFound(ex.Message);
//            }
//        }

//        [HttpPost("SendTestEmail")]
//        public ActionResult SendTestEmail(string to)
//        {
//            var body = mailService.RenderTemplate("RegistrationMail", new Dictionary<string, string>
//            {
//                { "Name", "Tobs" },
//                { "Date", DateTime.Now.ToString("MMMM dd, yyyy") },
//                { "Code", "Swiiiift!" }
//            });
//            mailService.SendEmail(to, "Test Email from Wavelength", body);
//            return Ok($"Test email sent to {to}");
//        }
//    }
//}
