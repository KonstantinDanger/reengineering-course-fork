using NetArchTest.Rules;

namespace NetSdrClientAppTests;

public class ArchitectureTests
{
    private readonly string _networkNamespace = "NetSdrClientApp.Networking";
    private readonly string _messagesNamespace = "NetSdrClientApp.Messages";

    [Test]
    public void Messages_ShouldNotDependOn_Networking()
    {
        var result = Types
            .InCurrentDomain()
            .That()
            .ResideInNamespace(_messagesNamespace)
            .ShouldNot()
            .HaveDependencyOn(_networkNamespace)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Networking_ShouldNotDependOn_Messages()
    {
        var result = Types
            .InCurrentDomain()
            .That()
            .ResideInNamespace(_networkNamespace)
            .ShouldNot()
            .HaveDependencyOn(_messagesNamespace)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }
}
