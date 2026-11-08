using LadyRanias.App.Components.Site.Api;
using LadyRanias.App.Components.Site.Impl;

namespace LadyRanias.App.Components.Site;

/// <summary>Det mamma styrer selv på siden som helhet: velkomstteksten, «Om oss» og forsidebildet.</summary>
public static class SiteComponent
{
    public static WebApplicationBuilder UseSiteComponent(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ISiteDatabase, TableSiteDatabase>();
        builder.Services.AddSingleton<ISiteHandler, SiteHandler>();

        return builder;
    }
}
