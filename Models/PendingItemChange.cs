namespace ProgressiveBotSystem.Models;

using Enums;

public record PendingItemChange(
    string Category,
    string Id,
    PendingItemAction Action,
    double Weight
);
