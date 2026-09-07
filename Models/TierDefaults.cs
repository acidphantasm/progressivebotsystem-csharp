namespace ProgressiveBotSystem.Models;

public static class TierDefaults
{
    public static readonly (int Tier, int PlayerMin, int PlayerMax)[] Ranges =
    [
        (1, 1, 10),
        (2, 11, 20),
        (3, 21, 30),
        (4, 31, 40),
        (5, 41, 50),
        (6, 51, 60),
        (7, 61, 100),
    ];

    public static readonly MinMax[] PmcDeltaDefaults =
    [
        new() { Min = 10, Max = 5 },
        new() { Min = 10, Max = 5 },
        new() { Min = 15, Max = 7 },
        new() { Min = 20, Max = 10 },
        new() { Min = 30, Max = 15 },
        new() { Min = 40, Max = 20 },
        new() { Min = 75, Max = 20 },
    ];

    public static readonly MinMax[] ScavDeltaDefaults =
    [
        new() { Min = 10, Max = 0 },
        new() { Min = 10, Max = -10 },
        new() { Min = 15, Max = -10 },
        new() { Min = 20, Max = -10 },
        new() { Min = 30, Max = -10 },
        new() { Min = 40, Max = -10 },
        new() { Min = 75, Max = -10 },
    ];

    public static readonly int[][] WeightDefaults =
    [
        [100, 20, 5, 0, 0, 0, 0],
        [40, 100, 20, 5, 0, 0, 0],
        [10, 40, 100, 20, 5, 0, 0],
        [5, 10, 40, 100, 20, 5, 0],
        [5, 5, 10, 40, 100, 20, 5],
        [5, 5, 5, 10, 40, 100, 20],
        [5, 5, 5, 5, 10, 40, 100],
    ];
}
