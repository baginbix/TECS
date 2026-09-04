using TECS;
using TECS.Commands;
using Xunit;

public class CommandBufferTests
{
    // Dummy components for testing
    public struct Position
    {
        public int X;
        public int Y;
    }

    public struct Velocity
    {
        public int Dx;
        public int Dy;
    }

    [Fact]
    public void Flush_ShouldDestroyQueuedEntities()
    {
        // Arrange
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        Entity e1 = ecs.CreateEntity();
        ecs.InsertComponent(e1, new Position { X = 10 });

        // Act - Queue destruction
        cmd.DestroyEntity(e1);

        // Assert BEFORE flush: The entity should still exist and have its component!
        var positionsBefore = ecs.QueryComponent<Position>();
        Assert.Single(positionsBefore);

        // Act - Flush
        cmd.Flush(ecs);

        // Assert AFTER flush: The entity and its components should be gone
        var positionsAfter = ecs.QueryComponent<Position>();
        Assert.Empty(positionsAfter);
    }

    [Fact]
    public void Flush_ShouldSpawnEntitiesAndMapFakeIDsToComponents()
    {
        // Arrange
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        // Act - Spawn through command buffer (generates fake ID)
        Entity fakeEntity = cmd.CreateEntity();

        // Assert the fake ID is negative
        Assert.True(fakeEntity.Id < 0);

        // Attach a component to the fake ID
        cmd.AddComponent(fakeEntity, new Position { X = 99 });

        // Assert BEFORE flush: ECS should have zero components
        Assert.Empty(ecs.QueryComponent<Position>());

        // Act - Flush to generate real ID and map the component
        cmd.Flush(ecs);

        // Assert AFTER flush: ECS should have the component on a real entity
        var positionsAfter = ecs.QueryComponent<Position>();
        Assert.Single(positionsAfter);
        Assert.Equal(99, positionsAfter[0].X);
    }

    [Fact]
    public void Flush_ShouldMapMultipleFakeIDsCorrectly()
    {
        // Arrange
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        // Act - Spawn two entities
        Entity fake1 = cmd.CreateEntity();
        Entity fake2 = cmd.CreateEntity();

        // Give them different values so we can verify they didn't overwrite each other
        cmd.AddComponent(fake1, new Position { X = 10 });
        cmd.AddComponent(fake2, new Position { X = 20 });

        cmd.Flush(ecs);

        // Assert
        var positions = ecs.QueryComponent<Position>();
        Assert.Equal(2, positions.Count);

        // Because of how ECS dense arrays work, they should both be there.
        // We just verify the sum or check for both values to ensure both mapped correctly.
        Assert.Contains(positions, p => p.X == 10);
        Assert.Contains(positions, p => p.X == 20);
    }

    [Fact]
    public void Flush_ShouldAddAndRemoveComponentsOnExistingEntities()
    {
        // Arrange
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        // Create a real entity immediately
        Entity realEntity = ecs.CreateEntity();
        ecs.InsertComponent(realEntity, new Position { X = 5 });

        // Act - Queue an addition and a removal
        cmd.AddComponent(realEntity, new Velocity { Dx = 2 });
        cmd.RemoveComponent<Position>(realEntity);

        // Assert BEFORE flush: Should only have Position, no Velocity
        Assert.Single(ecs.QueryComponent<Position>());
        Assert.Empty(ecs.QueryComponent<Velocity>());

        // Act - Flush
        cmd.Flush(ecs);

        // Assert AFTER flush: Position should be gone, Velocity should be added
        Assert.Empty(ecs.QueryComponent<Position>());
        var velocities = ecs.QueryComponent<Velocity>();
        Assert.Single(velocities);
        Assert.Equal(2, velocities[0].Dx);
    }

    [Fact]
    public void CommandBuffer_ShouldResolveContradictoryCommands()
    {
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        Entity e = ecs.CreateEntity();

        // The "Change of Heart": Add it, then immediately remove it before flushing
        cmd.AddComponent(e, new Position { X = 100 });
        cmd.RemoveComponent<Position>(e);

        cmd.Flush(ecs);

        // Assert the component does NOT exist
        Assert.Empty(ecs.GetComponentList<Position>());
    }

    [Fact]
    public void CommandBuffer_ShouldHandleDoubleDeletesGracefully()
    {
        var ecs = new ECS();
        var cmd = new CommandBuffer();

        Entity e = ecs.CreateEntity();

        // Two different systems both decide to destroy the same entity this frame
        cmd.DestroyEntity(e);
        cmd.DestroyEntity(e);

        // This flush should NOT crash! It should destroy it once and ignore the second.
        cmd.Flush(ecs);

        // To verify it didn't corrupt the state, try making a new entity
        Entity next = ecs.CreateEntity();
        Assert.True(next.Id >= 0);
    }
}
