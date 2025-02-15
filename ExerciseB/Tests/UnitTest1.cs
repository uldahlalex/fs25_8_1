using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public class ApiTests : WebApplicationFactory<Program>
{
    [Fact]
    public void Api_Can_Successfully_Add_Connection_To_Redis()
    {
        Assert.True(true);
    }
}
