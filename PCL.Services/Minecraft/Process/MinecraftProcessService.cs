using PCL.Services.Minecraft.Crash;
using PCL.Xsr;
using PCL.Xsr.State;

namespace PCL.Services.Minecraft.Process;

/// <summary>
/// Composition-phase state declaration for the Minecraft process capability.
/// </summary>
public sealed record MinecraftProcessFailure(Guid SessionId, string InstanceId, MinecraftLaunchFaultReport Report);

public static class MinecraftProcessStateComposition
{
    /// <summary>Ordered collection state key: snapshots keyed by session id.</summary>
    public static readonly XsrSemanticId SessionsKey = XsrSemanticId.Parse("minecraft.process.sessions");

    public static readonly XsrSemanticId FailuresKey = XsrSemanticId.Parse("minecraft.process.failures");

    public static void DeclareState(XsrStateStoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Collection<MinecraftProcessFailure, Guid>(FailuresKey, "PCL.Services.Minecraft.Process", static failure => failure.SessionId);
        builder.Collection<MinecraftProcessSnapshot, Guid>(
            SessionsKey,
            "PCL.Services.Minecraft.Process",
            static snapshot => snapshot.SessionId);
    }
}
