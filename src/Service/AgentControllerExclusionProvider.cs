using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Service;

/// <summary>
/// Removes agent controllers (those in <c>Service.Controllers.Agents</c>)
/// from the controller feature when no LLM is configured.
/// This prevents MVC from attempting to resolve <c>IChatClient</c>-dependent types.
/// </summary>
public class AgentControllerExclusionProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private const string AgentControllerNamespace = "Service.Controllers.Agents";

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        // Remove any controller whose namespace starts with the agent controller namespace.
        for (var i = feature.Controllers.Count - 1; i >= 0; i--)
        {
            var controllerType = feature.Controllers[i];
            if (controllerType.Namespace?.StartsWith(AgentControllerNamespace, StringComparison.Ordinal) == true)
            {
                feature.Controllers.RemoveAt(i);
            }
        }
    }
}
