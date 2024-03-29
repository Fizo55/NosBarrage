﻿namespace NosBarrage.Shared.Configuration;

public record LoginConfiguration : IConfiguration
{
    public string Address { get; init; }

    public int Port { get; init; }

    public string Database { get; init; }
}