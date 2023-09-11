using Sandbox;

using System;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Sandbox;

internal partial class Pawn : AnimatedEntity
{
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//
		// Use a watermelon model
		//
		Model = Cloud.Model( "https://asset.party/facepunch/watermelon" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	// An example BuildInput method within a player's Pawn class.
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		ViewAngles = viewAngles.Normal;
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		Rotation = ViewAngles.ToRotation();

		// build movement from the input values
		var movement = InputDirection.Normal;

		// rotate it to the direction we're facing
		Velocity = Rotation * movement;

		// apply some speed to it
		Velocity *= Input.Down( "run" ) ? 1000 : 200;

		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		MoveHelper helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace.Size( 16 );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
		}

		// If we're running serverside and Attack1 was just pressed, spawn a ragdoll
		if ( Game.IsServer && Input.Pressed( "attack1" ) )
		{
			var ragdoll = new ModelEntity();
			ragdoll.SetModel( "models/citizen/citizen.vmdl" );
			ragdoll.Position = Position + Rotation.Forward * 40;
			ragdoll.Rotation = Rotation.LookAt( Vector3.Random.Normal );
			ragdoll.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
			ragdoll.PhysicsGroup.Velocity = Rotation.Forward * 1000;
			ragdoll.DeleteAsync( 10.0f );
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = ViewAngles.ToRotation();

		Camera.Position = Position;
		Camera.Rotation = Rotation;

		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		// Set the first person viewer to this, so it won't render our model
		Camera.FirstPersonViewer = this;

		RayTrace();
	}

	private void RayTrace()
	{
		TraceResult _trace_result = Trace.Ray( AimRay, 1000.0f ).Run();

		if ( _trace_result.Hit )
		{
			DebugOverlay.ScreenText(
				$"Entity: {_trace_result.Entity}.\n" +
				$"Transform: {_trace_result.Entity.Transform}",
				 new Vector2( 100.0f, 100.0f ), 0, Color.Random, default );

			DebugOverlay.Sphere( _trace_result.EndPosition, 10.0f, Color.Random, default, true );
		}

		Vector3 directionOfStrike = Vector3.Forward * 100.0f /* some method to determine direction of strike */;
		Vector3 correctDirection = Vector3.Right * 100.0f /* direction indicated by the arrow on the cube */;

		DebugOverlay.Sphere( directionOfStrike, 100.0f, Color.Green, default, true );
		DebugOverlay.Sphere( correctDirection, 100.0f, Color.Blue, default, true );

		float dot = Vector3.Dot( directionOfStrike.Normal, correctDirection.Normal );
		if ( dot > 0.9f ) // Check if the direction of the strike is within some threshold of the correct direction
		{
			// Correct strike direction
		}
		else
		{
			// Incorrect strike direction
		}

		DebugOverlay.ScreenText( $"{dot}", new Vector2( 100.0f, 100.0f ), 5, Color.Random, default );
	}
}
