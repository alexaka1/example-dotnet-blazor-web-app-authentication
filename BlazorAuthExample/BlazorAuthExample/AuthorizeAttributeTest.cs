using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace BlazorAuthExample;

public static class AuthorizeAttributeTest
{
    /// <summary>
    ///     Verifies that all Blazor pages in the project have the appropriate authorization attributes.
    ///     It is intended to ensure that no pages are missing the [Authorize] attribute, except those
    ///     explicitly marked with [AllowAnonymous].
    ///     This method scans all assemblies in the application that start with the specified project name,
    ///     identifies Blazor pages by their [Route] attribute, and checks if they are correctly decorated
    ///     with the necessary authorization attributes.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to configure the application.</param>
    public static void TestAuthorizeAttributeOnPages(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                string? name = a.GetName().Name;
                Debug.Assert(name != null);
                return name.StartsWith(nameof(BlazorAuthExample));
            })
            .ToList();
        Debug.Assert(assemblies.Count > 0, "At least one project should be loaded.");

        // Find all Blazor pages across these assemblies.
        // A Blazor page is a ComponentBase with a [Route] attribute.
        var pages = assemblies
            .SelectMany(a =>
            {
                // Sometimes, not all assemblies can be scanned without error.
                // Use try/catch to skip problematic ones.
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null);
                }
            })
            .OfType<Type>()
            // Find all Blazor components (pages) that are decorated with [RouteAttribute]
            .Where(t => typeof(ComponentBase).IsAssignableFrom(t) &&
                        t.GetCustomAttributes(typeof(RouteAttribute), true).Length != 0)
            .ToList();

        Debug.Assert(pages.Count > 0, "At least one page should be found in the project.");

        // Optionally, if there are pages that are meant to be public,
        // and you decorate them with [AllowAnonymous] you might want to allow them:
        var pagesToCheck =
            pages.Where(t => t.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length == 0);

        // Find pages missing the [Authorize] attribute.
        var pagesMissingAuthorize = pagesToCheck
            .Where(t => t.GetCustomAttributes(typeof(AuthorizeAttribute), true).Length == 0)
            .Select(t => t.FullName)
            .ToList();

        // If any pages are missing the authorization attribute, fail the test and list them.
        string errorMessage = pagesMissingAuthorize.Count != 0 ?
            $"Pages missing [Authorize] attribute: {string.Join(", ", pagesMissingAuthorize)}" :
            string.Empty;

        Debug.Assert(pagesMissingAuthorize.Count == 0, errorMessage);
    }
}
