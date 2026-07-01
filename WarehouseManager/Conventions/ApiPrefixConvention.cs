using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace WarehouseManager.Conventions;

public class ApiPrefixConvention(IRouteTemplateProvider routeTemplateProvider) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix = new(routeTemplateProvider);

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel != null)
                {
                    // Combines the /api/ prefix with the existing controller route
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        _routePrefix, 
                        selector.AttributeRouteModel
                    );
                }
                else
                {
                    // Fallback if the controller doesn't have a [Route] attribute
                    selector.AttributeRouteModel = _routePrefix;
                }
            }
        }
    }
}