﻿using Revalee.Client;
using Revalee.Client.Mvc;
using Revalee.SampleSite.Infrastructure;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Revalee.SampleSite.Controllers
{
	[RevaleeClientSettings(RequestTimeout = 3000)]
	public class RevaleeTestController : Controller
	{
		private static CallbackLog _Log = new CallbackLog();

		private static int _TotalRequestCount = 0;

		private static int _TotalCallbackCount = 0;

		public ActionResult Index()
		{
			return View();
		}

		public ActionResult ViewLog()
		{
			ViewBag.TotalRequestCount = _TotalRequestCount;

			ViewBag.TotalCallbackCount = _TotalCallbackCount;

			return this.PartialView(_Log.Snapshot());
		}

		public ActionResult Schedule(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri)
		{
			DateTimeOffset now = DateTimeOffset.Now;

			Interlocked.Increment(ref _TotalRequestCount);

			Guid callbackId = RevaleeRegistrar.ScheduleCallback(serviceBaseUri, callbackTime, callbackUri);

			_Log.Add(callbackId, callbackTime, callbackUri, now);

			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		[RevaleeClientSettings(ServiceBaseUri = "http://localhost:46200", RequestTimeout = 3000)]	// This attribute is only needed if your need to override the web.config settings
		public async Task<ActionResult> ScheduleAsync(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri, CancellationToken cancellationToken)
		{
			DateTimeOffset now = DateTimeOffset.Now;

			Interlocked.Increment(ref _TotalRequestCount);

			// Supercede all configured values for ServiceBaseUri because the sample web page specifies this value at runtime.
			// Normally, this value would be configured in the web.config or by the RevaleeClientSettings attribute.
			RevaleeClientSettings.ServiceBaseUri = serviceBaseUri;

			Guid callbackId = await this.CallbackAtAsync(callbackUri, callbackTime, cancellationToken);

			_Log.Add(callbackId, callbackTime, callbackUri, now);

			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		[AllowAnonymous]
		[HttpPost]
		[CallbackAction]	// This attribute ensures that only legitimately requested callbacks get handled by this action
		public ActionResult Callback(Guid callbackId, DateTimeOffset callbackTime, DateTimeOffset currentServiceTime, string id)
		{
			DateTimeOffset now = DateTimeOffset.Now;

			Interlocked.Increment(ref _TotalCallbackCount);

			Task.Run(() => _Log.Update(callbackId, currentServiceTime, this.Request.Url, id, now));

			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			if (filterContext != null && filterContext.Exception != null)
			{
				System.Diagnostics.Debug.WriteLine("Error: " + filterContext.Exception.Message);
			}

			filterContext.ExceptionHandled = true;
			base.OnException(filterContext);
		}
	}
}