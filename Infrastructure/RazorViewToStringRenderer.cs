using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Semester03.Infrastructure
{
    public class RazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewToStringRenderer(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Render a Razor view to string. viewName can be: "Emails/TicketEmail" or "~/Views/Emails/TicketEmail.cshtml"
        /// </summary>
        public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var candidates = new[]
            {
                viewName,
                $"~/Views/{viewName}.cshtml",
                $"~/Views/{viewName.Replace("/", "\\")}.cshtml",
                $"~/Views/{viewName}.cshtml".Replace("\\", "/"),
                $"Views/{viewName}.cshtml",
                $"/Views/{viewName}.cshtml"
            };

            ViewEngineResult viewResult = null;
            foreach (var candidate in candidates)
            {
                viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: candidate, isMainPage: true);
                if (viewResult.Success) break;

                viewResult = _viewEngine.FindView(actionContext, candidate, isMainPage: true);
                if (viewResult.Success) break;
            }

            if (viewResult == null || !viewResult.Success)
            {
                var searched = string.Join(Environment.NewLine, viewResult?.SearchedLocations ?? Array.Empty<string>());
                throw new InvalidOperationException($"View '{viewName}' not found. Searched: {Environment.NewLine}{searched}");
            }

            await using var sw = new StringWriter();
            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };

            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, new TempDataDictionary(actionContext.HttpContext, _tempDataProvider), sw, new HtmlHelperOptions());
            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
