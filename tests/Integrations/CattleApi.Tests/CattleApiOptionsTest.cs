// <copyright file="CattleApiOptionsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Tests;

using Defra.Lis.Be4Fe.CattleApi;
using Microsoft.Extensions.Configuration;

public sealed class CattleApiOptionsTest
{
    [Fact]
    public void CattleApiOptionsShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new CattleApiOptions();

        // Assert
        CattleApiOptions.SectionName.ShouldBe("CattleApi");
        options.BaseUrl.ShouldBeNull();
        options.ApiKey.ShouldBeNull();
        options.GetHoldingDetailsTimeout.ShouldBe(60000);
        options.SearchHoldingsTimeout.ShouldBe(60000);
        options.SearchCattleTimeout.ShouldBe(60000);
        options.GetCattleDetailsTimeout.ShouldBe(60000);
        options.GetUserDetailsTimeout.ShouldBe(60000);
        options.CreateUserTimeout.ShouldBe(60000);
    }

    [Fact]
    public void CattleApiOptionsShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var options = new CattleApiOptions
        {
            BaseUrl = "https://cattle-api.service.gov.uk",
            ApiKey = "test123",
            GetHoldingDetailsTimeout = 1000,
            SearchHoldingsTimeout = 2000,
            SearchCattleTimeout = 3000,
            GetCattleDetailsTimeout = 4000,
            GetUserDetailsTimeout = 5000,
            CreateUserTimeout = 6000,
        };

        // Assert
        options.BaseUrl.ShouldBe("https://cattle-api.service.gov.uk");
        options.ApiKey.ShouldBe("test123");
        options.GetHoldingDetailsTimeout.ShouldBe(1000);
        options.SearchHoldingsTimeout.ShouldBe(2000);
        options.SearchCattleTimeout.ShouldBe(3000);
        options.GetCattleDetailsTimeout.ShouldBe(4000);
        options.GetUserDetailsTimeout.ShouldBe(5000);
        options.CreateUserTimeout.ShouldBe(6000);
    }

    [Fact]
    public void CattleApiOptionsShouldBindFromConfigurationSection()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["CattleApi:BaseUrl"] = "https://cattle-api.service.gov.uk",
            ["CattleApi:ApiKey"] = "config-api-key",
            ["CattleApi:GetHoldingDetailsTimeout"] = "15000",
            ["CattleApi:SearchHoldingsTimeout"] = "25000",
            ["CattleApi:SearchCattleTimeout"] = "35000",
            ["CattleApi:GetCattleDetailsTimeout"] = "45000",
            ["CattleApi:GetUserDetailsTimeout"] = "55000",
            ["CattleApi:CreateUserTimeout"] = "65000",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new CattleApiOptions();

        // Act
        configuration.GetRequiredSection(CattleApiOptions.SectionName).Bind(options);

        // Assert
        options.BaseUrl.ShouldBe("https://cattle-api.service.gov.uk");
        options.ApiKey.ShouldBe("config-api-key");
        options.GetHoldingDetailsTimeout.ShouldBe(15000);
        options.SearchHoldingsTimeout.ShouldBe(25000);
        options.SearchCattleTimeout.ShouldBe(35000);
        options.GetCattleDetailsTimeout.ShouldBe(45000);
        options.GetUserDetailsTimeout.ShouldBe(55000);
        options.CreateUserTimeout.ShouldBe(65000);
    }
}
