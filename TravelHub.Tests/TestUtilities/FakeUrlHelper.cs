using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace TravelHub.Tests.TestUtilities;

public class FakeUrlHelper : IUrlHelper
{
    private readonly ActionContext _actionContext;

    public FakeUrlHelper()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        // ustaw Host na coś sensownego, jeśli potrzebne w testach
        httpContext.Request.Host = new HostString("example");

        _actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }

    public ActionContext ActionContext => _actionContext;

    public string Action(UrlActionContext actionContext) => string.Empty;
    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => true;
    public string Link(string? routeName, object? values) => "http://example";
    public string RouteUrl(UrlRouteContext routeContext) => "http://example";

    public string Page(string pageName, string? pageHandler = null, object? values = null, string? protocol = null, string? host = null, string? fragment = null)
    {
        var usedProtocol = protocol ?? _actionContext.HttpContext.Request.Scheme ?? "https";
        var usedHost = !string.IsNullOrEmpty(host) ? host : (_actionContext.HttpContext.Request.Host.HasValue ? _actionContext.HttpContext.Request.Host.Value : "example");
        return $"{usedProtocol}://{usedHost}{pageName}";
    }
}
