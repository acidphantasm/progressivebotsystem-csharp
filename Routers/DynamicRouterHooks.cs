/*
 *
 * Completely unused until QB releases
 */

/*
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Routers;

[Injectable]
public class DynamicRouterHooks : DynamicRouter
{
    private static HttpResponseUtil _httpResponseUtil;
    private static ISptLogger<DynamicRouterHooks> _logger;
    
    public DynamicRouterHooks(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ISptLogger<DynamicRouterHooks> logger) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _httpResponseUtil = httpResponseUtil;
        _logger = logger;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<GenerateBotsRequestData>(
                "/QuestingBots/GenerateBot/",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) =>
                {
                    _logger.Success("Generate bot request data");
                    return await _botCallbacks.GenerateBots(url, info, sessionId);
                })
        ];
    }
}
*/