using TECS;

namespace IntegrationTests;

public class UnitTest1
{
    public enum MockStates
    {
        Start,
        Enter,
        Exit,
    }

    [Fact]
    public void StateTransitionEnter_SystemShouldRun()
    {
        var app = new App();
        app.AddState(MockStates.Enter);
    }
}
