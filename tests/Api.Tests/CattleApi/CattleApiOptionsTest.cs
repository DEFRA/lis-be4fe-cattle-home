// <copyright file="CattleApiOptionsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.CattleApi;

using Defra.Lis.Be4Fe.CattleApi;

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
}
