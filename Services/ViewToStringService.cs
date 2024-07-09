using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Chefster.Services;

public class ViewToStringService(
    ICompositeViewEngine viewEngine, 
    ITempDataProvider tempDataProvider, 
    IServiceProvider serviceProvider
)
{
    private readonly ICompositeViewEngine _viewEngine = viewEngine;
    private readonly ITempDataProvider _tempDataProvider = tempDataProvider;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<string> ViewToStringAsync(string viewName, object model)
    {
        var actionContext = GetActionContext();
        var view = FindView(actionContext, viewName);

        using (var stringWriter = new StringWriter())
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                tempData,
                stringWriter,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext);
            return stringWriter.ToString();
        }
    }

    private IView FindView(ActionContext actionContext, string viewName)
    {
        var getViewResult = _viewEngine.GetView(null, viewName, false);
        if (getViewResult.Success)
        {
            return getViewResult.View;
        }

        var findViewResult = _viewEngine.FindView(actionContext, viewName, false);
        if (findViewResult.Success)
        {
            return findViewResult.View;
        }

        var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
        var errorMessage = string.Join(
            Environment.NewLine,
            new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations));

        throw new InvalidOperationException(errorMessage);
    }

    private ActionContext GetActionContext()
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
