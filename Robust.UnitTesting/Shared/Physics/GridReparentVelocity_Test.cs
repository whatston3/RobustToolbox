using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

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
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: '-0.1,-0.1,0.1,0.1'
        hard: false
";

    // Moves an object off of a moving grid, checks for conservation of linear velocity.
    [Test]
    public async Task TestLinearVelocityOnlyMoveOffGrid()
    {
        var sim = RobustServerSimulation.NewSimulation();
        var server = sim.InitializeInstance();

        var systems = server.Resolve<IEntitySystemManager>();
        var entManager = server.Resolve<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid map = mapSystem.CreateMap(out var mapId);
        EntityUid grid = SetupTestGrid(mapId, server, systems, entManager, mapSystem);
        EntityUid obj = SetupTestObject(new EntityCoordinates(grid, 0.5f, 0.5f), physSystem, entManager);

        // Our object should start on the grid.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        
        // Set the velocity of the grid and our object.
        Assert.That(physSystem.SetLinearVelocity(obj, new Vector2(3.5f, 4.75f)), Is.True);
        Assert.That(physSystem.SetLinearVelocity(grid, new Vector2(1.0f, 2.0f)), Is.True);

        // Wait a second to clear the grid
        physSystem.Update(1.0f);

        // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        Assert.That(entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(4.5f, 6.75f)));
        Assert.That(entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
    }

    // Moves an object onto a moving grid, checks for conservation of linear velocity.
    [Test]
    public async Task TestLinearVelocityOnlyMoveOntoGrid()
    {
        var sim = RobustServerSimulation.NewSimulation();
        var server = sim.InitializeInstance();

        var systems = server.Resolve<IEntitySystemManager>();
        var entManager = server.Resolve<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

        // Spawn our test object 1 m off of the middle of the grid in both directions, ensure it has no damping.
        EntityUid map = mapSystem.CreateMap(out var mapId);
        EntityUid grid = SetupTestGrid(mapId, server, systems, entManager, mapSystem);
        EntityUid obj = SetupTestObject(new EntityCoordinates(map, 1.5f, 1.5f), physSystem, entManager);

        // Assert that we start off the grid.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        
        // Set the velocity of the grid and our object.
        Assert.That(physSystem.SetLinearVelocity(obj, new Vector2(-2.0f, -3.0f)), Is.True);
        Assert.That(physSystem.SetLinearVelocity(grid, new Vector2(-1.0f, -2.0f)), Is.True);

        // Wait a second to move onto the middle of the grid
        physSystem.Update(1.0f);

        // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        Assert.That(entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -1.0f)));
        Assert.That(entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
    }

    // Moves a rotating object off of a rotating grid, checks for conservation of angular velocity.
    [Test]
    public async Task TestLinearAndAngularVelocityMoveOffGrid()
    {
        var sim = RobustServerSimulation.NewSimulation();
        var server = sim.InitializeInstance();

        var systems = server.Resolve<IEntitySystemManager>();
        var entManager = server.Resolve<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = systems.GetEntitySystem<SharedMapSystem>();

        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid map = mapSystem.CreateMap(out var mapId);
        EntityUid grid = SetupTestGrid(mapId, server, systems, entManager, mapSystem);
        EntityUid obj = SetupTestObject(new EntityCoordinates(grid, 0.5f, 0.5f), physSystem, entManager);

        // Our object should start on the grid.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        
        // Set the velocity of the grid and our object.
        Assert.That(physSystem.SetLinearVelocity(obj, new Vector2(3.5f, 4.75f)), Is.True);
        Assert.That(physSystem.SetAngularVelocity(obj, 1.0f), Is.True);
        Assert.That(physSystem.SetLinearVelocity(grid, new Vector2(1.0f, 2.0f)), Is.True);
        Assert.That(physSystem.SetAngularVelocity(grid, 2.0f), Is.True);

        // Wait a second to clear the grid
        physSystem.Update(1.0f);

        // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
        Assert.That(entManager.GetComponent<PhysicsComponent>(obj).AngularVelocity, Is.EqualTo(3.0f));
        var gridPhys = entManager.GetComponent<PhysicsComponent>(grid);
        Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
    }

    // Moves a rotating object onto a rotating grid, checks for conservation of angular velocity.
    [Test]
    public async Task TestLinearAndAngularVelocityMoveOntoGrid()
    {
        var sim = RobustServerSimulation.NewSimulation();
        var server = sim.InitializeInstance();

        var systems = server.Resolve<IEntitySystemManager>();
        var entManager = server.Resolve<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = systems.GetEntitySystem<SharedMapSystem>();

        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid map = mapSystem.CreateMap(out var mapId);
        EntityUid grid = SetupTestGrid(mapId, server, systems, entManager, mapSystem);
        EntityUid obj = SetupTestObject(new EntityCoordinates(map, 1.5f, 1.5f), physSystem, entManager);

        // Assert that we start off the grid.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        
        // Set the velocity of the grid and our object.
        Assert.That(physSystem.SetLinearVelocity(obj, new Vector2(-2.0f, -3.0f)), Is.True);
        Assert.That(physSystem.SetAngularVelocity(obj, 1.0f), Is.True);
        Assert.That(physSystem.SetLinearVelocity(grid, new Vector2(-1.0f, -2.0f)), Is.True);
        Assert.That(physSystem.SetAngularVelocity(grid, 2.0f), Is.True);

        // Wait a second to move onto the middle of the grid
        physSystem.Update(1.0f);

        // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
        Assert.That(entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
        Assert.That(entManager.GetComponent<PhysicsComponent>(obj).AngularVelocity, Is.EqualTo(-1.0f));
        var gridPhys = entManager.GetComponent<PhysicsComponent>(grid);
        Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
        Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
    }

    // Spawn a 1x1 grid centered at (0.5, 0.5), ensure it's movable and its velocity has no damping.
    public EntityUid SetupTestGrid(MapId map, ISimulation server, IEntitySystemManager systems, IEntityManager entManager, SharedMapSystem mapSystem)
    {
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();
        var mapManager = server.Resolve<IMapManager>();

        var gridEnt = mapManager.CreateGridEntity(map);
        physSystem.SetCanCollide(gridEnt, true);
        physSystem.SetBodyType(gridEnt, BodyType.Dynamic);
        var gridPhys = entManager.GetComponent<PhysicsComponent>(gridEnt);
        physSystem.SetLinearDamping(gridEnt, gridPhys, 0.0f);
        physSystem.SetAngularDamping(gridEnt, gridPhys, 0.0f);

        mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
        return gridEnt.Owner;
    }

    // Spawn a test object at the given position, ensure its velocity has no damping.
    public EntityUid SetupTestObject(EntityCoordinates coords, SharedPhysicsSystem physSystem, IEntityManager entManager)
    {
        var obj = entManager.SpawnEntity("ReparentTestObject", coords);
        physSystem.SetCanCollide(obj, true);
        physSystem.SetLinearDamping(obj, entManager.GetComponent<PhysicsComponent>(obj), 0.0f);
        physSystem.SetAngularDamping(obj, entManager.GetComponent<PhysicsComponent>(obj), 0.0f);

        return obj;
    }
}
