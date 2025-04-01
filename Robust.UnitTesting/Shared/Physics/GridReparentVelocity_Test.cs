using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture, TestOf(typeof(SharedPhysicsSystem))]
public sealed class GridReparentVelocity_Test : RobustIntegrationTest
{
    private static readonly string Prototypes = @"
- type: entity
  id: ReparentTestObject
  components:
  - type: Physics
    bodyType: Dynamic
";

    [Test]
    public async Task TestLinearVelocityMoveOffGrid()
    {
        var serverOpts = new ServerIntegrationOptions { Pool = false, ExtraPrototypes = Prototypes };
        var server = StartServer(serverOpts);

        await server.WaitIdleAsync();

        // Checks that FindGridContacts succesfully overlaps a grid + map broadphase physics body
        var systems = server.ResolveDependency<IEntitySystemManager>();
        var fixtureSystem = systems.GetEntitySystem<FixtureSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var transformSystem = entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

        // Set up entities
        EntityUid map = default;
        EntityUid grid = default;
        EntityUid obj = default;
        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            // Spawn a grid with one tile.
            var gridEnt = mapManager.CreateGridEntity(mapId);
            mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
            grid = gridEnt.Owner;

            // Spawn our test object in the middle of the grid.
            obj = server.EntMan.SpawnEntity("ReparentTestObject", new EntityCoordinates(grid, 0.5f, 0.5f));
            physSystem.SetCanCollide(obj, true);
        });

        await server.WaitAssertion(() =>
        {
            // Our object should start on the grid.
            Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
            
            // Set the velocity of the grid and our object.
            physSystem.SetLinearVelocity(grid, new Vector2(1.0f, 2.0f));
            physSystem.SetLinearVelocity(obj, new Vector2(3.5f, 4.75f));

            // Wait a second to clear the grid
            physSystem.Update(1.0f);

            // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
            Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
            Assert.That(entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(4.5f, 6.75f)));
            Assert.That(entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        });
    }

    [Test]
    public async Task TestLinearVelocityMoveOntoGrid()
    {
        var serverOpts = new ServerIntegrationOptions { Pool = false, ExtraPrototypes = Prototypes };
        var server = StartServer(serverOpts);

        await server.WaitIdleAsync();

        // Checks that FindGridContacts succesfully overlaps a grid + map broadphase physics body
        var systems = server.ResolveDependency<IEntitySystemManager>();
        var fixtureSystem = systems.GetEntitySystem<FixtureSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var transformSystem = entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

        // Set up entities
        EntityUid map = default;
        EntityUid grid = default;
        EntityUid obj = default;
        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            // Spawn a grid with one tile.
            var gridEnt = mapManager.CreateGridEntity(mapId);
            mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
            grid = gridEnt.Owner;

            // Spawn our test object 1 m off of the middle of the grid in both directions.
            obj = server.EntMan.SpawnEntity("ReparentTestObject", new EntityCoordinates(map, 1.5f, 1.5f));
            physSystem.SetCanCollide(obj, true);
        });

        await server.WaitAssertion(() =>
        {
            // Assert that we start off the grid.
            Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
            
            // Set the velocity of the grid and our object.
            physSystem.SetLinearVelocity(grid, new Vector2(-1.0f, -2.0f));
            physSystem.SetLinearVelocity(obj, new Vector2(-2.0f, -3.0f));

            // Wait a second to move onto the middle of the grid
            physSystem.Update(1.0f);

            // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
            Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
            Assert.That(entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -1.0f)));
            Assert.That(entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(-2.0f, -3.0f)));
        });
    }
}
