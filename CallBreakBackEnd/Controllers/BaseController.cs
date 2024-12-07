using CallBreakBackEnd.Models.DTO.Output;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Net;
using System.Net.Mail;

namespace CallBreakBackEnd.Controllers
{

    public class BaseController : ControllerBase
	{
		protected new OkObjectResult Ok()
		{
			return base.Ok(new BaseJsonResponse
			{
				Success = true
			});
		}

		protected new OkObjectResult Ok([ActionResultObjectValue] object? value)
		{
			return base.Ok(new BaseJsonResponse
			{
				Success = true,
				Data = value
			});
		}

		protected ObjectResult Error(HttpStatusCode httpStatusCode, string? message = null)
		{
			BaseJsonResponse retval = new BaseJsonResponse
			{
				Success = false,
				Data=message
			};

			

			return base.StatusCode((int)httpStatusCode, retval);
		}

		public override CreatedAtActionResult CreatedAtAction(string? actionName, [ActionResultObjectValue] object? value)
		{
			return base.CreatedAtAction(actionName,new BaseJsonResponse
			{
				Success = true,
				Data = value
			});
		}
	}
}
