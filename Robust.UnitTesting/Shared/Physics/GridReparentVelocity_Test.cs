using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
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

    private ISimulation _sim = default!;
    private IEntitySystemManager _systems = default!;
    private IEntityManager _entManager = default!;
    private IMapManager _mapManager = default!;
    private SharedPhysicsSystem _physSystem = default!;
    private SharedMapSystem _mapSystem = default!;

    // Test objects.
    private EntityUid _mapUid = default!;
    private EntityUid _gridUid = default!;
    private EntityUid _objUid = default!;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        _sim = RobustServerSimulation.NewSimulation()
            .RegisterPrototypes(protoMan => protoMan.LoadString(Prototypes))
            .InitializeInstance();

        _systems = _sim.Resolve<IEntitySystemManager>();
        _entManager = _sim.Resolve<IEntityManager>();
        _mapManager = _sim.Resolve<IMapManager>();
        _physSystem = _systems.GetEntitySystem<SharedPhysicsSystem>();
        _mapSystem = _systems.GetEntitySystem<SharedMapSystem>();
    }

    [SetUp]
    public void Setup()
    {
        _mapUid = _mapSystem.CreateMap(out var mapId);
        _gridUid = SetupTestGrid(mapId);
    }

    [TearDown]
    public void Teardown()
    {
        _entManager.DeleteEntity(_gridUid);
        _gridUid = default!;
        _entManager.DeleteEntity(_objUid);
        _objUid = default!;
    }

    // Moves an object off of a moving grid, checks for conservation of linear velocity.
    [Test]
    public async Task TestLinearVelocityOnlyMoveOffGrid()
    {
        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid obj = SetupTestObject(new EntityCoordinates(grid, 0.5f, 0.5f));

        // Our object should start on the grid.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        
        // Set the velocity of the grid and our object.
        Assert.That(_physSystem.SetLinearVelocity(obj, new Vector2(3.5f, 4.75f)), Is.True);
        Assert.That(_physSystem.SetLinearVelocity(grid, new Vector2(1.0f, 2.0f)), Is.True);

        // Wait a second to clear the grid
        _physSystem.Update(1.0f);

        // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        Assert.That(_entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(4.5f, 6.75f)));
        Assert.That(_entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
    }

    [Test]
    // Moves an object onto a moving grid, checks for conservation of linear velocity.
    public async Task TestLinearVelocityOnlyMoveOntoGrid()
    {
        // Spawn our test object 1 m off of the middle of the grid in both directions, ensure it has no damping.
        EntityUid obj = SetupTestObject(new EntityCoordinates(map, 1.5f, 1.5f));

        // Assert that we start off the grid.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        
        // Set the velocity of the grid and our object.
        Assert.That(_physSystem.SetLinearVelocity(obj, new Vector2(-2.0f, -3.0f)), Is.True);
        Assert.That(_physSystem.SetLinearVelocity(grid, new Vector2(-1.0f, -2.0f)), Is.True);

        // Wait a second to move onto the middle of the grid
        _physSystem.Update(1.0f);

        // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        Assert.That(_entManager.GetComponent<PhysicsComponent>(obj).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -1.0f)));
        Assert.That(_entManager.GetComponent<PhysicsComponent>(grid).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
    }

    [Test]
    // Moves a rotating object off of a rotating grid, checks for conservation of angular velocity.
    public async Task TestLinearAndAngularVelocityMoveOffGrid()
    {
        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid obj = SetupTestObject(new EntityCoordinates(grid, 0.5f, 0.5f));

        // Our object should start on the grid.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        
        // Set the velocity of the grid and our object.
        Assert.That(_physSystem.SetLinearVelocity(obj, new Vector2(3.5f, 4.75f)), Is.True);
        Assert.That(_physSystem.SetAngularVelocity(obj, 1.0f), Is.True);
        Assert.That(_physSystem.SetLinearVelocity(grid, new Vector2(1.0f, 2.0f)), Is.True);
        Assert.That(_physSystem.SetAngularVelocity(grid, 2.0f), Is.True);

        // Wait a second to clear the grid
        _physSystem.Update(1.0f);

        // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
        Assert.That(_entManager.GetComponent<PhysicsComponent>(obj).AngularVelocity, Is.EqualTo(3.0f));
        var gridPhys = _entManager.GetComponent<PhysicsComponent>(grid);
        Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
    }

    [Test]
    // Moves a rotating object onto a rotating grid, checks for conservation of angular velocity.
    public async Task TestLinearAndAngularVelocityMoveOntoGrid()
    {
        // Spawn our test object in the middle of the grid, ensure it has no damping.
        EntityUid obj = SetupTestObject(new EntityCoordinates(map, 1.5f, 1.5f));

        // Assert that we start off the grid.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(map));
        
        // Set the velocity of the grid and our object.
        Assert.That(_physSystem.SetLinearVelocity(obj, new Vector2(-2.0f, -3.0f)), Is.True);
        Assert.That(_physSystem.SetAngularVelocity(obj, 1.0f), Is.True);
        Assert.That(_physSystem.SetLinearVelocity(grid, new Vector2(-1.0f, -2.0f)), Is.True);
        Assert.That(_physSystem.SetAngularVelocity(grid, 2.0f), Is.True);

        // Wait a second to move onto the middle of the grid
        _physSystem.Update(1.0f);

        // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
        Assert.That(_entManager.GetComponent<TransformComponent>(obj).ParentUid, Is.EqualTo(grid));
        // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
        Assert.That(_entManager.GetComponent<PhysicsComponent>(obj).AngularVelocity, Is.EqualTo(-1.0f));
        var gridPhys = _entManager.GetComponent<PhysicsComponent>(grid);
        Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
        Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
    }

    // Spawn a 1x1 grid centered at (0.5, 0.5), ensure it's movable and its velocity has no damping.
    public EntityUid SetupTestGrid(MapId map)
    {
        var physSystem = _systems.GetEntitySystem<SharedPhysicsSystem>();

        var gridEnt = _mapManager.CreateGridEntity(map);
        physSystem.SetCanCollide(gridEnt, true);
        physSystem.SetBodyType(gridEnt, BodyType.Dynamic);
        var gridPhys = _entManager.GetComponent<PhysicsComponent>(gridEnt);
        physSystem.SetLinearDamping(gridEnt, gridPhys, 0.0f);
        physSystem.SetAngularDamping(gridEnt, gridPhys, 0.0f);

        _mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
        return gridEnt.Owner;
    }

    // Spawn a test object at the given position, ensure its velocity has no damping.
    public EntityUid SetupTestObject(EntityCoordinates coords)
    {
        var obj = _entManager.SpawnEntity("ReparentTestObject", coords);
        _physSystem.SetCanCollide(obj, true);
        _physSystem.SetLinearDamping(obj, _entManager.GetComponent<PhysicsComponent>(obj), 0.0f);
        _physSystem.SetAngularDamping(obj, _entManager.GetComponent<PhysicsComponent>(obj), 0.0f);

        return obj;
    }
}
