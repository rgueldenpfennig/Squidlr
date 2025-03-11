namespace Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderSecurityExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this WebApplication app)
    {
        return app.UseSecurityHeaders(policies =>
                policies.AddDefaultSecurityHeaders()
                        .AddContentSecurityPolicy(builder =>
                        {
                            builder.AddDefaultSrc().None();

                            if (app.Environment.IsDevelopment())
                            {
                                builder.AddConnectSrc()
                                       .Self()
                                       .From("http://localhost:*")
                                       .From("https://localhost:*")
                                       .From("ws://localhost:*")
                                       .From("wss://localhost:*");
                            }
                            else
                            {
                                builder.AddConnectSrc().Self();
                            }

                            var storageKofiUrl = "https://storage.ko-fi.com";

                            builder.AddBaseUri().Self();
                            builder.AddFontSrc().Self();
                            builder.AddFormAction().Self();
                            builder.AddFrameAncestors().None();
                            builder.AddFrameSrc().From("https://ko-fi.com/");
                            builder.AddImgSrc().Self().From("https://*.tiktokcdn.com")
                                                      .From("https://*.tiktokcdn-us.com")
                                                      .From("https://*.tiktokcdn-eu.com")
                                                      .From("https://media.licdn.com")
                                                      .From("https://*.fbcdn.net")
                                                      .From("https://pbs.twimg.com")
                                                      .From(storageKofiUrl);
                            builder.AddObjectSrc().None();
                            builder.AddScriptSrc().Self().From(storageKofiUrl).UnsafeInline();
                            builder.AddStyleSrc().Self().From(storageKofiUrl).UnsafeInline();
                            builder.AddStyleSrcAttr().Self().From(storageKofiUrl).UnsafeInline();
                            builder.AddStyleSrcElem().Self().From(storageKofiUrl).UnsafeInline();
                        })
                        .AddCustomHeader("Strict-Transport-Security", $"max-age={TimeSpan.FromDays(2 * 365).TotalSeconds}; includeSubDomains; preload")
            );
    }
}
